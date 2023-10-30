#define THRESHOLD 1e-7

struct CollisionInfo
{
    float3 pos;
    float3 normal;
    float3 impulse;
};

struct Plane
{
    float3 normal;
    float3 pos;
};

bool planeRaycast(in Plane plane, float3 origin, float3 direction, out float enter)
{
    float d = dot(plane.normal, direction);

    if (abs(d) <= THRESHOLD)
    {
        enter = 0.0;
        return false;
    }

    enter = dot(plane.normal, plane.pos - origin) / d;
    if (enter >= 0.0) return true;

    return false;
}

bool planeSide(in Plane plane, float3 pos)
{
    const float3 toPoint = pos - plane.pos;
    return dot(toPoint, plane.normal) >= 0.0;
}

struct Triangle
{
    float3 v1;
    float3 v2;
    float3 v3;
};

float3 triangleNormal(in Triangle tri)
{
    return cross(tri.v1 - tri.v2, tri.v1 - tri.v3);
}

void triangleFaceDirection(inout Triangle tri, float3 dir)
{
    if (dot(triangleNormal(tri), dir) > 0.0) return;
    float3 v = tri.v1;
    tri.v1 = tri.v3;
    tri.v3 = v;
}
