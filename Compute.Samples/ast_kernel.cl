__kernel void SimpleAstKernel_method_100663314(__global float* input, __global float* output, const uint count) {
    int gid = get_global_id(0);
    if ((gid >= count)) return;
    float value = input[gid];
    output[gid] = ((value * 2.000f) + 1.000f);
}

