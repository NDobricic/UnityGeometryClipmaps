float2 hash(float2 p) {
    const float3 K3 = float3(0.3333333, 0.1666667, 0.0);
    float3 p3 = frac(float3(p.x, p.y, p.x) * K3.x + K3.y);
    p3 += dot(p3, p3.zxy + K3.y);
    return frac((p3.xxy + p3.yzz) * (p3.yzz + p3.xxy));
}

//uint hashint(uint a)
//{
//    a = a ^ (a>>4);
//    a = (a^0xdeadbeef) + (a<<5);
//    a = a ^ (a>>11);
//    return a;
//}

//float2 hash(float2 p)
//{
//    uint2 ui = uint2(asuint(p.x), asuint(p.y));

//    uint hashed = hash((ui.y<<8) + ui.x) & 0xff;

//    return float2(hashedValue, hashint(hashedValue + 1)) / float(0xFFFFFFFF) * 2.0 - 1.0;
//}

// return gradient noise (in x) and its derivatives (in yz)
float3 noised(float2 p)
{
    int2 i = floor(p);
    float2 f = frac(p);

    // quintic interpolation
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float2 du = 30.0 * f * f * (f * (f - 2.0) + 1.0);

    float2 ga = hash(i + float2(0.0, 0.0));
    float2 gb = hash(i + float2(1.0, 0.0));
    float2 gc = hash(i + float2(0.0, 1.0));
    float2 gd = hash(i + float2(1.0, 1.0));

    float va = dot(ga, f - float2(0.0, 0.0));
    float vb = dot(gb, f - float2(1.0, 0.0));
    float vc = dot(gc, f - float2(0.0, 1.0));
    float vd = dot(gd, f - float2(1.0, 1.0));

    return float3(va + u.x * (vb - va) + u.y * (vc - va) + u.x * u.y * (va - vb - vc + vd), // value
                  ga + u.x * (gb - ga) + u.y * (gc - ga) + u.x * u.y * (ga - gb - gc + gd) + // derivatives
                  du * (u.yx * (va - vb - vc + vd) + float2(vb, vc) - va));
}
