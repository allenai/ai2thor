float3 orientCubePointToSegmentWithWidth(float3 localPt, float3 p0, float3 p1, float width0, float width1)
{
    float3 localSpaceVert = localPt;

    float len = length(p1 - p0);
    float3 up = float3(0.0f, 1.0f, 0.0f);
    float3 forward = normalize(p1 - p0);
    if(len < 0.0001f) // near zero length line segment
    {
        forward = float3(1.0f, 0.0f, 0.0f);
    }
    if(abs(forward.y - 1.0f) < 0.00001f)
    {
        up = float3(1.0f, 0.0f, 0.0f);
    }

    // Create LookAt matrix
    float3 zaxis = forward;
    float3 xaxis = normalize(cross(up, zaxis));
    float3 yaxis = cross(zaxis, xaxis);

    float4x4 lookatMatrix = {
        xaxis.x, yaxis.x, zaxis.x, p0.x,
        xaxis.y, yaxis.y, zaxis.y, p0.y,
        xaxis.z, yaxis.z, zaxis.z, p0.z,
        0,       0,       0,       1
    };

    if(localSpaceVert.z > 0.0f)
    {
        localSpaceVert.xy *= width0;
        localSpaceVert.z = len + width0/2.0f;
    }
    else
    {
        localSpaceVert.xy *= width1;
        localSpaceVert.z = -1.0f * width1/2.0f;
    }

    // Apply LookAt matrix
    return mul(lookatMatrix, float4(localSpaceVert, 1.0)).xyz;
}
