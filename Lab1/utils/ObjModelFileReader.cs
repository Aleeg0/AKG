using System.Globalization;
using System.IO;
using System.Numerics;
using Lab1.model;

namespace Lab1.utils;

public class ObjModelFileReader
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static ObjModel ReadModel(string filePath)
    {
        ObjModel objModel = new ObjModel();

        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var allTokens = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (allTokens.Length == 0)
                continue;

            var type = allTokens[0];
            var payloadTokens = allTokens.Skip(1).ToArray();

            switch (type)
            {
                case "v":
                    objModel.VtxsGeometric.Add(ReadGeometrixVertex(payloadTokens));
                    break;
                case "vt":
                    objModel.VtxsTexture.Add(ReadTextureVertex(payloadTokens));
                    break;
                case "vn":
                    objModel.VtxsNormal.Add(ReadNormalVertex(payloadTokens));
                    break;
                case "f":
                    objModel.Polygons.Add(ReadPolygon(payloadTokens, objModel));
                    break;
            }
        }

        return objModel;
    }

    private static Vector4 ReadGeometrixVertex(string[] tokens)
    {
        if (tokens.Length < 3) throw new ArgumentNullException(nameof(tokens));

        float x = float.Parse(tokens[0], Culture);
        float y = float.Parse(tokens[1], Culture);
        float z = float.Parse(tokens[2], Culture);
        float w = tokens.Length == 4 ? float.Parse(tokens[3], Culture) : 1.0f;

        return new Vector4(x, y, z, w);
    }

    private static Vector3 ReadTextureVertex(string[] tokens)
    {
        if (tokens.Length < 1) throw new ArgumentNullException(nameof(tokens));

        float u = float.Parse(tokens[0], Culture);
        float v = tokens.Length == 2 ? float.Parse(tokens[1], Culture) : 0.0f;
        float w = tokens.Length == 3 ? float.Parse(tokens[2], Culture) : 0.0f;

        return new Vector3(u, v, w);
    }

    private static Vector3 ReadNormalVertex(string[] tokens)
    {
        if (tokens.Length < 3) throw new ArgumentNullException(nameof(tokens));

        float i = float.Parse(tokens[0], Culture);
        float j = float.Parse(tokens[1], Culture);
        float k = float.Parse(tokens[2], Culture);

        return new Vector3(i, j, k);
    }

    private static Polygon ReadPolygon(string[] tokens, ObjModel model)
    {
        if (tokens.Length < 3) throw new ArgumentNullException(nameof(tokens));

        var polygon = new Polygon();

        for (int i = 0; i < tokens.Length; i++)
        {
            var vertex = tokens[i];
            var vartexToken = vertex.Split('/');

            var vertexIndices = new PolygonVertexes();

            if (vartexToken.Length > 0)
            {
                vertexIndices.GeometrixIndex = ParseIndex(vartexToken[0], model.VtxsGeometric.Count);
            }

            if (vartexToken.Length > 1 && vartexToken[1].Length > 0)
            {
                vertexIndices.TextureIndex = ParseIndex(vartexToken[1], model.VtxsTexture.Count);
            }

            if (vartexToken.Length > 2)
            {
                vertexIndices.NormalIndex = ParseIndex(vartexToken[2], model.VtxsNormal.Count);
            }

            polygon.PolygonRecords.Add(vertexIndices);
        }

        return polygon;
    }

    private static int ParseIndex(string token, int totalCount)
    {
        int index = int.Parse(token, Culture);
        return index > 0 ?  index - 1 : totalCount + index;
    }
}