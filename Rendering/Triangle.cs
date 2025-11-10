namespace RasterizerCube;

public struct Triangle
{
    public Vertex V0, V1, V2;
    public Texture Texture;

    public Triangle(Vertex v0, Vertex v1, Vertex v2, Texture texture)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
        Texture = texture;
    }
}
