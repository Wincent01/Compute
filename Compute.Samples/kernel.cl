typedef struct Matrix4x4

{

	float M11;
	float M12;
	float M13;
	float M14;
	float M21;
	float M22;
	float M23;
	float M24;
	float M31;
	float M32;
	float M33;
	float M34;
	float M41;
	float M42;
	float M43;
	float M44;


}  Matrix4x4
;
__kernel void ExampleKernel(__global struct Matrix4x4* input, __global struct Matrix4x4* output, const unsigned int count);
Matrix4x4 Multiply( struct Matrix4x4 value1,  struct Matrix4x4 value2);
__kernel void ExampleKernel(__global struct Matrix4x4* input, __global struct Matrix4x4* output, const unsigned int count)
{
	int local0 = 0;
	Matrix4x4 local1;
	int local2 = 0;
	int local3 = 0;
	int local4 = 0;

IL0: // IL_0000: nop
IL1: // IL_0001: ldc.i4.0
IL2: // IL_0002: call System.Int32 Compute.IL.CLFunctions::GetGlobalId(System.Int32)
IL7: // IL_0007: stloc.0
	local0 = (int) (get_global_id((int) 0));
IL8: // IL_0008: ldloc.0
IL9: // IL_0009: conv.i8
IL10: // IL_000a: ldarg.2
IL11: // IL_000b: conv.u8
IL12: // IL_000c: clt
IL14: // IL_000e: ldc.i4.0
IL15: // IL_000f: ceq
IL17: // IL_0011: stloc.2
	local2 = (int) (((((int) (unsigned long long) (count)) > ((int) (long long) (local0))) == 0));
IL18: // IL_0012: ldloc.2
IL19: // IL_0013: brfalse.s IL_0017
	if (((unsigned char) local2) == 0) goto IL23;
IL21: // IL_0015: br.s IL_0044
	goto IL68;
IL23: // IL_0017: ldarg.0
IL24: // IL_0018: ldloc.0
IL25: // IL_0019: ldelem.any System.Numerics.Matrix4x4
IL30: // IL_001e: stloc.1
	local1 = (Matrix4x4) ((input[local0]));
IL31: // IL_001f: ldc.i4.0
IL32: // IL_0020: stloc.3
	local3 = (int) (0);
IL33: // IL_0021: br.s IL_0031
	goto IL49;
IL35: // IL_0023: nop
IL36: // IL_0024: ldloc.1
IL37: // IL_0025: ldloc.1
IL38: // IL_0026: call System.Numerics.Matrix4x4 Compute.Samples.Program::Multiply(System.Numerics.Matrix4x4,System.Numerics.Matrix4x4)
IL43: // IL_002b: stloc.1
	local1 = (Matrix4x4) (Multiply((Matrix4x4) local1, (Matrix4x4) local1));
IL44: // IL_002c: nop
IL45: // IL_002d: ldloc.3
IL46: // IL_002e: ldc.i4.1
IL47: // IL_002f: add
IL48: // IL_0030: stloc.3
	local3 = (int) ((1 + local3));
IL49: // IL_0031: ldloc.3
IL50: // IL_0032: ldc.i4.s 100
IL52: // IL_0034: clt
IL54: // IL_0036: stloc.s V_4
	local4 = (int) ((((int) 100) > ((int) local3)));
IL56: // IL_0038: ldloc.s V_4
IL58: // IL_003a: brtrue.s IL_0023
	if (((unsigned char) local4) != 0) goto IL35;
IL60: // IL_003c: ldarg.1
IL61: // IL_003d: ldloc.0
IL62: // IL_003e: ldloc.1
IL63: // IL_003f: stelem.any System.Numerics.Matrix4x4
	output[local0] = (local1);
IL68: // IL_0044: ret
	return;


}


Matrix4x4 Multiply( struct Matrix4x4 value1,  struct Matrix4x4 value2)
{
	Matrix4x4 local0;
	Matrix4x4 local1;

IL0: // IL_0000: nop
IL1: // IL_0001: ldloca.s V_0
IL3: // IL_0003: ldarg.0
IL4: // IL_0004: ldfld System.Single System.Numerics.Matrix4x4::M11
IL9: // IL_0009: ldarg.1
IL10: // IL_000a: ldfld System.Single System.Numerics.Matrix4x4::M11
IL15: // IL_000f: mul
IL16: // IL_0010: ldarg.0
IL17: // IL_0011: ldfld System.Single System.Numerics.Matrix4x4::M12
IL22: // IL_0016: ldarg.1
IL23: // IL_0017: ldfld System.Single System.Numerics.Matrix4x4::M21
IL28: // IL_001c: mul
IL29: // IL_001d: add
IL30: // IL_001e: ldarg.0
IL31: // IL_001f: ldfld System.Single System.Numerics.Matrix4x4::M13
IL36: // IL_0024: ldarg.1
IL37: // IL_0025: ldfld System.Single System.Numerics.Matrix4x4::M31
IL42: // IL_002a: mul
IL43: // IL_002b: add
IL44: // IL_002c: ldarg.0
IL45: // IL_002d: ldfld System.Single System.Numerics.Matrix4x4::M14
IL50: // IL_0032: ldarg.1
IL51: // IL_0033: ldfld System.Single System.Numerics.Matrix4x4::M41
IL56: // IL_0038: mul
IL57: // IL_0039: add
IL58: // IL_003a: stfld System.Single System.Numerics.Matrix4x4::M11
	(&(local0))->M11 = (((((value2).M41)) * (((value1).M14))) + (((((value2).M31)) * (((value1).M13))) + (((((value2).M21)) * (((value1).M12))) + ((((value2).M11)) * (((value1).M11))))));
IL63: // IL_003f: ldloca.s V_0
IL65: // IL_0041: ldarg.0
IL66: // IL_0042: ldfld System.Single System.Numerics.Matrix4x4::M11
IL71: // IL_0047: ldarg.1
IL72: // IL_0048: ldfld System.Single System.Numerics.Matrix4x4::M12
IL77: // IL_004d: mul
IL78: // IL_004e: ldarg.0
IL79: // IL_004f: ldfld System.Single System.Numerics.Matrix4x4::M12
IL84: // IL_0054: ldarg.1
IL85: // IL_0055: ldfld System.Single System.Numerics.Matrix4x4::M22
IL90: // IL_005a: mul
IL91: // IL_005b: add
IL92: // IL_005c: ldarg.0
IL93: // IL_005d: ldfld System.Single System.Numerics.Matrix4x4::M13
IL98: // IL_0062: ldarg.1
IL99: // IL_0063: ldfld System.Single System.Numerics.Matrix4x4::M32
IL104: // IL_0068: mul
IL105: // IL_0069: add
IL106: // IL_006a: ldarg.0
IL107: // IL_006b: ldfld System.Single System.Numerics.Matrix4x4::M14
IL112: // IL_0070: ldarg.1
IL113: // IL_0071: ldfld System.Single System.Numerics.Matrix4x4::M42
IL118: // IL_0076: mul
IL119: // IL_0077: add
IL120: // IL_0078: stfld System.Single System.Numerics.Matrix4x4::M12
	(&(local0))->M12 = (((((value2).M42)) * (((value1).M14))) + (((((value2).M32)) * (((value1).M13))) + (((((value2).M22)) * (((value1).M12))) + ((((value2).M12)) * (((value1).M11))))));
IL125: // IL_007d: ldloca.s V_0
IL127: // IL_007f: ldarg.0
IL128: // IL_0080: ldfld System.Single System.Numerics.Matrix4x4::M11
IL133: // IL_0085: ldarg.1
IL134: // IL_0086: ldfld System.Single System.Numerics.Matrix4x4::M13
IL139: // IL_008b: mul
IL140: // IL_008c: ldarg.0
IL141: // IL_008d: ldfld System.Single System.Numerics.Matrix4x4::M12
IL146: // IL_0092: ldarg.1
IL147: // IL_0093: ldfld System.Single System.Numerics.Matrix4x4::M23
IL152: // IL_0098: mul
IL153: // IL_0099: add
IL154: // IL_009a: ldarg.0
IL155: // IL_009b: ldfld System.Single System.Numerics.Matrix4x4::M13
IL160: // IL_00a0: ldarg.1
IL161: // IL_00a1: ldfld System.Single System.Numerics.Matrix4x4::M33
IL166: // IL_00a6: mul
IL167: // IL_00a7: add
IL168: // IL_00a8: ldarg.0
IL169: // IL_00a9: ldfld System.Single System.Numerics.Matrix4x4::M14
IL174: // IL_00ae: ldarg.1
IL175: // IL_00af: ldfld System.Single System.Numerics.Matrix4x4::M43
IL180: // IL_00b4: mul
IL181: // IL_00b5: add
IL182: // IL_00b6: stfld System.Single System.Numerics.Matrix4x4::M13
	(&(local0))->M13 = (((((value2).M43)) * (((value1).M14))) + (((((value2).M33)) * (((value1).M13))) + (((((value2).M23)) * (((value1).M12))) + ((((value2).M13)) * (((value1).M11))))));
IL187: // IL_00bb: ldloca.s V_0
IL189: // IL_00bd: ldarg.0
IL190: // IL_00be: ldfld System.Single System.Numerics.Matrix4x4::M11
IL195: // IL_00c3: ldarg.1
IL196: // IL_00c4: ldfld System.Single System.Numerics.Matrix4x4::M14
IL201: // IL_00c9: mul
IL202: // IL_00ca: ldarg.0
IL203: // IL_00cb: ldfld System.Single System.Numerics.Matrix4x4::M12
IL208: // IL_00d0: ldarg.1
IL209: // IL_00d1: ldfld System.Single System.Numerics.Matrix4x4::M24
IL214: // IL_00d6: mul
IL215: // IL_00d7: add
IL216: // IL_00d8: ldarg.0
IL217: // IL_00d9: ldfld System.Single System.Numerics.Matrix4x4::M13
IL222: // IL_00de: ldarg.1
IL223: // IL_00df: ldfld System.Single System.Numerics.Matrix4x4::M34
IL228: // IL_00e4: mul
IL229: // IL_00e5: add
IL230: // IL_00e6: ldarg.0
IL231: // IL_00e7: ldfld System.Single System.Numerics.Matrix4x4::M14
IL236: // IL_00ec: ldarg.1
IL237: // IL_00ed: ldfld System.Single System.Numerics.Matrix4x4::M44
IL242: // IL_00f2: mul
IL243: // IL_00f3: add
IL244: // IL_00f4: stfld System.Single System.Numerics.Matrix4x4::M14
	(&(local0))->M14 = (((((value2).M44)) * (((value1).M14))) + (((((value2).M34)) * (((value1).M13))) + (((((value2).M24)) * (((value1).M12))) + ((((value2).M14)) * (((value1).M11))))));
IL249: // IL_00f9: ldloca.s V_0
IL251: // IL_00fb: ldarg.0
IL252: // IL_00fc: ldfld System.Single System.Numerics.Matrix4x4::M21
IL257: // IL_0101: ldarg.1
IL258: // IL_0102: ldfld System.Single System.Numerics.Matrix4x4::M11
IL263: // IL_0107: mul
IL264: // IL_0108: ldarg.0
IL265: // IL_0109: ldfld System.Single System.Numerics.Matrix4x4::M22
IL270: // IL_010e: ldarg.1
IL271: // IL_010f: ldfld System.Single System.Numerics.Matrix4x4::M21
IL276: // IL_0114: mul
IL277: // IL_0115: add
IL278: // IL_0116: ldarg.0
IL279: // IL_0117: ldfld System.Single System.Numerics.Matrix4x4::M23
IL284: // IL_011c: ldarg.1
IL285: // IL_011d: ldfld System.Single System.Numerics.Matrix4x4::M31
IL290: // IL_0122: mul
IL291: // IL_0123: add
IL292: // IL_0124: ldarg.0
IL293: // IL_0125: ldfld System.Single System.Numerics.Matrix4x4::M24
IL298: // IL_012a: ldarg.1
IL299: // IL_012b: ldfld System.Single System.Numerics.Matrix4x4::M41
IL304: // IL_0130: mul
IL305: // IL_0131: add
IL306: // IL_0132: stfld System.Single System.Numerics.Matrix4x4::M21
	(&(local0))->M21 = (((((value2).M41)) * (((value1).M24))) + (((((value2).M31)) * (((value1).M23))) + (((((value2).M21)) * (((value1).M22))) + ((((value2).M11)) * (((value1).M21))))));
IL311: // IL_0137: ldloca.s V_0
IL313: // IL_0139: ldarg.0
IL314: // IL_013a: ldfld System.Single System.Numerics.Matrix4x4::M21
IL319: // IL_013f: ldarg.1
IL320: // IL_0140: ldfld System.Single System.Numerics.Matrix4x4::M12
IL325: // IL_0145: mul
IL326: // IL_0146: ldarg.0
IL327: // IL_0147: ldfld System.Single System.Numerics.Matrix4x4::M22
IL332: // IL_014c: ldarg.1
IL333: // IL_014d: ldfld System.Single System.Numerics.Matrix4x4::M22
IL338: // IL_0152: mul
IL339: // IL_0153: add
IL340: // IL_0154: ldarg.0
IL341: // IL_0155: ldfld System.Single System.Numerics.Matrix4x4::M23
IL346: // IL_015a: ldarg.1
IL347: // IL_015b: ldfld System.Single System.Numerics.Matrix4x4::M32
IL352: // IL_0160: mul
IL353: // IL_0161: add
IL354: // IL_0162: ldarg.0
IL355: // IL_0163: ldfld System.Single System.Numerics.Matrix4x4::M24
IL360: // IL_0168: ldarg.1
IL361: // IL_0169: ldfld System.Single System.Numerics.Matrix4x4::M42
IL366: // IL_016e: mul
IL367: // IL_016f: add
IL368: // IL_0170: stfld System.Single System.Numerics.Matrix4x4::M22
	(&(local0))->M22 = (((((value2).M42)) * (((value1).M24))) + (((((value2).M32)) * (((value1).M23))) + (((((value2).M22)) * (((value1).M22))) + ((((value2).M12)) * (((value1).M21))))));
IL373: // IL_0175: ldloca.s V_0
IL375: // IL_0177: ldarg.0
IL376: // IL_0178: ldfld System.Single System.Numerics.Matrix4x4::M21
IL381: // IL_017d: ldarg.1
IL382: // IL_017e: ldfld System.Single System.Numerics.Matrix4x4::M13
IL387: // IL_0183: mul
IL388: // IL_0184: ldarg.0
IL389: // IL_0185: ldfld System.Single System.Numerics.Matrix4x4::M22
IL394: // IL_018a: ldarg.1
IL395: // IL_018b: ldfld System.Single System.Numerics.Matrix4x4::M23
IL400: // IL_0190: mul
IL401: // IL_0191: add
IL402: // IL_0192: ldarg.0
IL403: // IL_0193: ldfld System.Single System.Numerics.Matrix4x4::M23
IL408: // IL_0198: ldarg.1
IL409: // IL_0199: ldfld System.Single System.Numerics.Matrix4x4::M33
IL414: // IL_019e: mul
IL415: // IL_019f: add
IL416: // IL_01a0: ldarg.0
IL417: // IL_01a1: ldfld System.Single System.Numerics.Matrix4x4::M24
IL422: // IL_01a6: ldarg.1
IL423: // IL_01a7: ldfld System.Single System.Numerics.Matrix4x4::M43
IL428: // IL_01ac: mul
IL429: // IL_01ad: add
IL430: // IL_01ae: stfld System.Single System.Numerics.Matrix4x4::M23
	(&(local0))->M23 = (((((value2).M43)) * (((value1).M24))) + (((((value2).M33)) * (((value1).M23))) + (((((value2).M23)) * (((value1).M22))) + ((((value2).M13)) * (((value1).M21))))));
IL435: // IL_01b3: ldloca.s V_0
IL437: // IL_01b5: ldarg.0
IL438: // IL_01b6: ldfld System.Single System.Numerics.Matrix4x4::M21
IL443: // IL_01bb: ldarg.1
IL444: // IL_01bc: ldfld System.Single System.Numerics.Matrix4x4::M14
IL449: // IL_01c1: mul
IL450: // IL_01c2: ldarg.0
IL451: // IL_01c3: ldfld System.Single System.Numerics.Matrix4x4::M22
IL456: // IL_01c8: ldarg.1
IL457: // IL_01c9: ldfld System.Single System.Numerics.Matrix4x4::M24
IL462: // IL_01ce: mul
IL463: // IL_01cf: add
IL464: // IL_01d0: ldarg.0
IL465: // IL_01d1: ldfld System.Single System.Numerics.Matrix4x4::M23
IL470: // IL_01d6: ldarg.1
IL471: // IL_01d7: ldfld System.Single System.Numerics.Matrix4x4::M34
IL476: // IL_01dc: mul
IL477: // IL_01dd: add
IL478: // IL_01de: ldarg.0
IL479: // IL_01df: ldfld System.Single System.Numerics.Matrix4x4::M24
IL484: // IL_01e4: ldarg.1
IL485: // IL_01e5: ldfld System.Single System.Numerics.Matrix4x4::M44
IL490: // IL_01ea: mul
IL491: // IL_01eb: add
IL492: // IL_01ec: stfld System.Single System.Numerics.Matrix4x4::M24
	(&(local0))->M24 = (((((value2).M44)) * (((value1).M24))) + (((((value2).M34)) * (((value1).M23))) + (((((value2).M24)) * (((value1).M22))) + ((((value2).M14)) * (((value1).M21))))));
IL497: // IL_01f1: ldloca.s V_0
IL499: // IL_01f3: ldarg.0
IL500: // IL_01f4: ldfld System.Single System.Numerics.Matrix4x4::M31
IL505: // IL_01f9: ldarg.1
IL506: // IL_01fa: ldfld System.Single System.Numerics.Matrix4x4::M11
IL511: // IL_01ff: mul
IL512: // IL_0200: ldarg.0
IL513: // IL_0201: ldfld System.Single System.Numerics.Matrix4x4::M32
IL518: // IL_0206: ldarg.1
IL519: // IL_0207: ldfld System.Single System.Numerics.Matrix4x4::M21
IL524: // IL_020c: mul
IL525: // IL_020d: add
IL526: // IL_020e: ldarg.0
IL527: // IL_020f: ldfld System.Single System.Numerics.Matrix4x4::M33
IL532: // IL_0214: ldarg.1
IL533: // IL_0215: ldfld System.Single System.Numerics.Matrix4x4::M31
IL538: // IL_021a: mul
IL539: // IL_021b: add
IL540: // IL_021c: ldarg.0
IL541: // IL_021d: ldfld System.Single System.Numerics.Matrix4x4::M34
IL546: // IL_0222: ldarg.1
IL547: // IL_0223: ldfld System.Single System.Numerics.Matrix4x4::M41
IL552: // IL_0228: mul
IL553: // IL_0229: add
IL554: // IL_022a: stfld System.Single System.Numerics.Matrix4x4::M31
	(&(local0))->M31 = (((((value2).M41)) * (((value1).M34))) + (((((value2).M31)) * (((value1).M33))) + (((((value2).M21)) * (((value1).M32))) + ((((value2).M11)) * (((value1).M31))))));
IL559: // IL_022f: ldloca.s V_0
IL561: // IL_0231: ldarg.0
IL562: // IL_0232: ldfld System.Single System.Numerics.Matrix4x4::M31
IL567: // IL_0237: ldarg.1
IL568: // IL_0238: ldfld System.Single System.Numerics.Matrix4x4::M12
IL573: // IL_023d: mul
IL574: // IL_023e: ldarg.0
IL575: // IL_023f: ldfld System.Single System.Numerics.Matrix4x4::M32
IL580: // IL_0244: ldarg.1
IL581: // IL_0245: ldfld System.Single System.Numerics.Matrix4x4::M22
IL586: // IL_024a: mul
IL587: // IL_024b: add
IL588: // IL_024c: ldarg.0
IL589: // IL_024d: ldfld System.Single System.Numerics.Matrix4x4::M33
IL594: // IL_0252: ldarg.1
IL595: // IL_0253: ldfld System.Single System.Numerics.Matrix4x4::M32
IL600: // IL_0258: mul
IL601: // IL_0259: add
IL602: // IL_025a: ldarg.0
IL603: // IL_025b: ldfld System.Single System.Numerics.Matrix4x4::M34
IL608: // IL_0260: ldarg.1
IL609: // IL_0261: ldfld System.Single System.Numerics.Matrix4x4::M42
IL614: // IL_0266: mul
IL615: // IL_0267: add
IL616: // IL_0268: stfld System.Single System.Numerics.Matrix4x4::M32
	(&(local0))->M32 = (((((value2).M42)) * (((value1).M34))) + (((((value2).M32)) * (((value1).M33))) + (((((value2).M22)) * (((value1).M32))) + ((((value2).M12)) * (((value1).M31))))));
IL621: // IL_026d: ldloca.s V_0
IL623: // IL_026f: ldarg.0
IL624: // IL_0270: ldfld System.Single System.Numerics.Matrix4x4::M31
IL629: // IL_0275: ldarg.1
IL630: // IL_0276: ldfld System.Single System.Numerics.Matrix4x4::M13
IL635: // IL_027b: mul
IL636: // IL_027c: ldarg.0
IL637: // IL_027d: ldfld System.Single System.Numerics.Matrix4x4::M32
IL642: // IL_0282: ldarg.1
IL643: // IL_0283: ldfld System.Single System.Numerics.Matrix4x4::M23
IL648: // IL_0288: mul
IL649: // IL_0289: add
IL650: // IL_028a: ldarg.0
IL651: // IL_028b: ldfld System.Single System.Numerics.Matrix4x4::M33
IL656: // IL_0290: ldarg.1
IL657: // IL_0291: ldfld System.Single System.Numerics.Matrix4x4::M33
IL662: // IL_0296: mul
IL663: // IL_0297: add
IL664: // IL_0298: ldarg.0
IL665: // IL_0299: ldfld System.Single System.Numerics.Matrix4x4::M34
IL670: // IL_029e: ldarg.1
IL671: // IL_029f: ldfld System.Single System.Numerics.Matrix4x4::M43
IL676: // IL_02a4: mul
IL677: // IL_02a5: add
IL678: // IL_02a6: stfld System.Single System.Numerics.Matrix4x4::M33
	(&(local0))->M33 = (((((value2).M43)) * (((value1).M34))) + (((((value2).M33)) * (((value1).M33))) + (((((value2).M23)) * (((value1).M32))) + ((((value2).M13)) * (((value1).M31))))));
IL683: // IL_02ab: ldloca.s V_0
IL685: // IL_02ad: ldarg.0
IL686: // IL_02ae: ldfld System.Single System.Numerics.Matrix4x4::M31
IL691: // IL_02b3: ldarg.1
IL692: // IL_02b4: ldfld System.Single System.Numerics.Matrix4x4::M14
IL697: // IL_02b9: mul
IL698: // IL_02ba: ldarg.0
IL699: // IL_02bb: ldfld System.Single System.Numerics.Matrix4x4::M32
IL704: // IL_02c0: ldarg.1
IL705: // IL_02c1: ldfld System.Single System.Numerics.Matrix4x4::M24
IL710: // IL_02c6: mul
IL711: // IL_02c7: add
IL712: // IL_02c8: ldarg.0
IL713: // IL_02c9: ldfld System.Single System.Numerics.Matrix4x4::M33
IL718: // IL_02ce: ldarg.1
IL719: // IL_02cf: ldfld System.Single System.Numerics.Matrix4x4::M34
IL724: // IL_02d4: mul
IL725: // IL_02d5: add
IL726: // IL_02d6: ldarg.0
IL727: // IL_02d7: ldfld System.Single System.Numerics.Matrix4x4::M34
IL732: // IL_02dc: ldarg.1
IL733: // IL_02dd: ldfld System.Single System.Numerics.Matrix4x4::M44
IL738: // IL_02e2: mul
IL739: // IL_02e3: add
IL740: // IL_02e4: stfld System.Single System.Numerics.Matrix4x4::M34
	(&(local0))->M34 = (((((value2).M44)) * (((value1).M34))) + (((((value2).M34)) * (((value1).M33))) + (((((value2).M24)) * (((value1).M32))) + ((((value2).M14)) * (((value1).M31))))));
IL745: // IL_02e9: ldloca.s V_0
IL747: // IL_02eb: ldarg.0
IL748: // IL_02ec: ldfld System.Single System.Numerics.Matrix4x4::M41
IL753: // IL_02f1: ldarg.1
IL754: // IL_02f2: ldfld System.Single System.Numerics.Matrix4x4::M11
IL759: // IL_02f7: mul
IL760: // IL_02f8: ldarg.0
IL761: // IL_02f9: ldfld System.Single System.Numerics.Matrix4x4::M42
IL766: // IL_02fe: ldarg.1
IL767: // IL_02ff: ldfld System.Single System.Numerics.Matrix4x4::M21
IL772: // IL_0304: mul
IL773: // IL_0305: add
IL774: // IL_0306: ldarg.0
IL775: // IL_0307: ldfld System.Single System.Numerics.Matrix4x4::M43
IL780: // IL_030c: ldarg.1
IL781: // IL_030d: ldfld System.Single System.Numerics.Matrix4x4::M31
IL786: // IL_0312: mul
IL787: // IL_0313: add
IL788: // IL_0314: ldarg.0
IL789: // IL_0315: ldfld System.Single System.Numerics.Matrix4x4::M44
IL794: // IL_031a: ldarg.1
IL795: // IL_031b: ldfld System.Single System.Numerics.Matrix4x4::M41
IL800: // IL_0320: mul
IL801: // IL_0321: add
IL802: // IL_0322: stfld System.Single System.Numerics.Matrix4x4::M41
	(&(local0))->M41 = (((((value2).M41)) * (((value1).M44))) + (((((value2).M31)) * (((value1).M43))) + (((((value2).M21)) * (((value1).M42))) + ((((value2).M11)) * (((value1).M41))))));
IL807: // IL_0327: ldloca.s V_0
IL809: // IL_0329: ldarg.0
IL810: // IL_032a: ldfld System.Single System.Numerics.Matrix4x4::M41
IL815: // IL_032f: ldarg.1
IL816: // IL_0330: ldfld System.Single System.Numerics.Matrix4x4::M12
IL821: // IL_0335: mul
IL822: // IL_0336: ldarg.0
IL823: // IL_0337: ldfld System.Single System.Numerics.Matrix4x4::M42
IL828: // IL_033c: ldarg.1
IL829: // IL_033d: ldfld System.Single System.Numerics.Matrix4x4::M22
IL834: // IL_0342: mul
IL835: // IL_0343: add
IL836: // IL_0344: ldarg.0
IL837: // IL_0345: ldfld System.Single System.Numerics.Matrix4x4::M43
IL842: // IL_034a: ldarg.1
IL843: // IL_034b: ldfld System.Single System.Numerics.Matrix4x4::M32
IL848: // IL_0350: mul
IL849: // IL_0351: add
IL850: // IL_0352: ldarg.0
IL851: // IL_0353: ldfld System.Single System.Numerics.Matrix4x4::M44
IL856: // IL_0358: ldarg.1
IL857: // IL_0359: ldfld System.Single System.Numerics.Matrix4x4::M42
IL862: // IL_035e: mul
IL863: // IL_035f: add
IL864: // IL_0360: stfld System.Single System.Numerics.Matrix4x4::M42
	(&(local0))->M42 = (((((value2).M42)) * (((value1).M44))) + (((((value2).M32)) * (((value1).M43))) + (((((value2).M22)) * (((value1).M42))) + ((((value2).M12)) * (((value1).M41))))));
IL869: // IL_0365: ldloca.s V_0
IL871: // IL_0367: ldarg.0
IL872: // IL_0368: ldfld System.Single System.Numerics.Matrix4x4::M41
IL877: // IL_036d: ldarg.1
IL878: // IL_036e: ldfld System.Single System.Numerics.Matrix4x4::M13
IL883: // IL_0373: mul
IL884: // IL_0374: ldarg.0
IL885: // IL_0375: ldfld System.Single System.Numerics.Matrix4x4::M42
IL890: // IL_037a: ldarg.1
IL891: // IL_037b: ldfld System.Single System.Numerics.Matrix4x4::M23
IL896: // IL_0380: mul
IL897: // IL_0381: add
IL898: // IL_0382: ldarg.0
IL899: // IL_0383: ldfld System.Single System.Numerics.Matrix4x4::M43
IL904: // IL_0388: ldarg.1
IL905: // IL_0389: ldfld System.Single System.Numerics.Matrix4x4::M33
IL910: // IL_038e: mul
IL911: // IL_038f: add
IL912: // IL_0390: ldarg.0
IL913: // IL_0391: ldfld System.Single System.Numerics.Matrix4x4::M44
IL918: // IL_0396: ldarg.1
IL919: // IL_0397: ldfld System.Single System.Numerics.Matrix4x4::M43
IL924: // IL_039c: mul
IL925: // IL_039d: add
IL926: // IL_039e: stfld System.Single System.Numerics.Matrix4x4::M43
	(&(local0))->M43 = (((((value2).M43)) * (((value1).M44))) + (((((value2).M33)) * (((value1).M43))) + (((((value2).M23)) * (((value1).M42))) + ((((value2).M13)) * (((value1).M41))))));
IL931: // IL_03a3: ldloca.s V_0
IL933: // IL_03a5: ldarg.0
IL934: // IL_03a6: ldfld System.Single System.Numerics.Matrix4x4::M41
IL939: // IL_03ab: ldarg.1
IL940: // IL_03ac: ldfld System.Single System.Numerics.Matrix4x4::M14
IL945: // IL_03b1: mul
IL946: // IL_03b2: ldarg.0
IL947: // IL_03b3: ldfld System.Single System.Numerics.Matrix4x4::M42
IL952: // IL_03b8: ldarg.1
IL953: // IL_03b9: ldfld System.Single System.Numerics.Matrix4x4::M24
IL958: // IL_03be: mul
IL959: // IL_03bf: add
IL960: // IL_03c0: ldarg.0
IL961: // IL_03c1: ldfld System.Single System.Numerics.Matrix4x4::M43
IL966: // IL_03c6: ldarg.1
IL967: // IL_03c7: ldfld System.Single System.Numerics.Matrix4x4::M34
IL972: // IL_03cc: mul
IL973: // IL_03cd: add
IL974: // IL_03ce: ldarg.0
IL975: // IL_03cf: ldfld System.Single System.Numerics.Matrix4x4::M44
IL980: // IL_03d4: ldarg.1
IL981: // IL_03d5: ldfld System.Single System.Numerics.Matrix4x4::M44
IL986: // IL_03da: mul
IL987: // IL_03db: add
IL988: // IL_03dc: stfld System.Single System.Numerics.Matrix4x4::M44
	(&(local0))->M44 = (((((value2).M44)) * (((value1).M44))) + (((((value2).M34)) * (((value1).M43))) + (((((value2).M24)) * (((value1).M42))) + ((((value2).M14)) * (((value1).M41))))));
IL993: // IL_03e1: ldloc.0
IL994: // IL_03e2: stloc.1
	local1 = (Matrix4x4) (local0);
IL995: // IL_03e3: br.s IL_03e5
	goto IL997;
IL997: // IL_03e5: ldloc.1
IL998: // IL_03e6: ret
	return local1;


}

