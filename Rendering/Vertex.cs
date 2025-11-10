namespace RasterizerCube;

public struct Vertex
{
    public Float3 Position;
    public Float3 Normal;
    public Float2 TexCoord;

    public Vertex(Float3 position, Float3 normal, Float2 texCoord)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
    }
}
