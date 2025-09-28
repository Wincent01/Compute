typedef struct Vector3

{

	float X;
	float Y;
	float Z;


}  Vector3
;
typedef struct Body

{

	float Mass;
	Vector3 Position;
	Vector3 Velocity;


}  Body
;
__kernel void Simulate_method_100663309(__global struct Body* inBodies, __global struct Body* outBodies, const unsigned int bodyCount, const float deltaTime);
__kernel void Simulate_method_100663309(__global struct Body* inBodies, __global struct Body* outBodies, const unsigned int bodyCount, const float deltaTime)
{
	int local0 = 0;
	Body local1;
	float local2 = 0;
	float local3 = 0;
	float local4 = 0;
	int local5 = 0;
	float local6 = 0;
	int local7 = 0;
	int local8 = 0;
	Body local9;
	float local10 = 0;
	float local11 = 0;
	float local12 = 0;
	float local13 = 0;
	float local14 = 0;
	float local15 = 0;
	float local16 = 0;
	int local17 = 0;
	int local18 = 0;

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
IL17: // IL_0011: stloc.s V_7
	local7 = (int) (((((int) (unsigned long long) (bodyCount)) > ((int) (long long) (local0))) == 0));
IL19: // IL_0013: ldloc.s V_7
IL21: // IL_0015: brfalse.s IL_001c
	if (((unsigned char) local7) == 0) goto IL28;
IL23: // IL_0017: br IL_0209
	goto IL521;
IL28: // IL_001c: ldarg.0
IL29: // IL_001d: ldloc.0
IL30: // IL_001e: ldelem.any Compute.Samples.Body
IL35: // IL_0023: stloc.1
	local1 = (Body) ((inBodies[local0]));
IL36: // IL_0024: ldc.r4 0
IL41: // IL_0029: stloc.2
	local2 = (float) (((float) 0.0f));
IL42: // IL_002a: ldc.r4 0
IL47: // IL_002f: stloc.3
	local3 = (float) (((float) 0.0f));
IL48: // IL_0030: ldc.r4 0
IL53: // IL_0035: stloc.s V_4
	local4 = (float) (((float) 0.0f));
IL55: // IL_0037: ldarg.2
IL56: // IL_0038: stloc.s V_5
	local5 = (int) (bodyCount);
IL58: // IL_003a: ldc.i4.0
IL59: // IL_003b: stloc.s V_8
	local8 = (int) (0);
IL61: // IL_003d: br IL_011a
	goto IL282;
IL66: // IL_0042: nop
IL67: // IL_0043: ldloc.s V_8
IL69: // IL_0045: ldloc.0
IL70: // IL_0046: ceq
IL72: // IL_0048: stloc.s V_17
	local17 = (int) ((local8 == local0));
IL74: // IL_004a: ldloc.s V_17
IL76: // IL_004c: brfalse.s IL_0053
	if (((unsigned char) local17) == 0) goto IL83;
IL78: // IL_004e: br IL_0114
	goto IL276;
IL83: // IL_0053: ldarg.0
IL84: // IL_0054: ldloc.s V_8
IL86: // IL_0056: ldelem.any Compute.Samples.Body
IL91: // IL_005b: stloc.s V_9
	local9 = (Body) ((inBodies[local8]));
IL93: // IL_005d: ldloc.s V_9
IL95: // IL_005f: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL100: // IL_0064: ldfld System.Single Compute.Samples.Vector3::X
IL105: // IL_0069: ldloc.1
IL106: // IL_006a: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL111: // IL_006f: ldfld System.Single Compute.Samples.Vector3::X
IL116: // IL_0074: sub
IL117: // IL_0075: stloc.s V_10
	local10 = (float) ((((((local9).Position)).X) - ((((local1).Position)).X)));
IL119: // IL_0077: ldloc.s V_9
IL121: // IL_0079: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL126: // IL_007e: ldfld System.Single Compute.Samples.Vector3::Y
IL131: // IL_0083: ldloc.1
IL132: // IL_0084: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL137: // IL_0089: ldfld System.Single Compute.Samples.Vector3::Y
IL142: // IL_008e: sub
IL143: // IL_008f: stloc.s V_11
	local11 = (float) ((((((local9).Position)).Y) - ((((local1).Position)).Y)));
IL145: // IL_0091: ldloc.s V_9
IL147: // IL_0093: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL152: // IL_0098: ldfld System.Single Compute.Samples.Vector3::Z
IL157: // IL_009d: ldloc.1
IL158: // IL_009e: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL163: // IL_00a3: ldfld System.Single Compute.Samples.Vector3::Z
IL168: // IL_00a8: sub
IL169: // IL_00a9: stloc.s V_12
	local12 = (float) ((((((local9).Position)).Z) - ((((local1).Position)).Z)));
IL171: // IL_00ab: ldloc.s V_10
IL173: // IL_00ad: ldloc.s V_10
IL175: // IL_00af: mul
IL176: // IL_00b0: ldloc.s V_11
IL178: // IL_00b2: ldloc.s V_11
IL180: // IL_00b4: mul
IL181: // IL_00b5: add
IL182: // IL_00b6: ldloc.s V_12
IL184: // IL_00b8: ldloc.s V_12
IL186: // IL_00ba: mul
IL187: // IL_00bb: add
IL188: // IL_00bc: ldc.r4 1E-09
IL193: // IL_00c1: add
IL194: // IL_00c2: stloc.s V_13
	local13 = (float) ((((float) 1E-09f) + (((local12) * (local12)) + (((local11) * (local11)) + ((local10) * (local10))))));
IL196: // IL_00c4: ldloc.s V_13
IL198: // IL_00c6: call System.Single Compute.IL.CLFunctions::Sqrt(System.Single)
IL203: // IL_00cb: stloc.s V_14
	local14 = (float) (sqrt((float) local13));
IL205: // IL_00cd: ldc.r4 6.6743E-11
IL210: // IL_00d2: ldloc.1
IL211: // IL_00d3: ldfld System.Single Compute.Samples.Body::Mass
IL216: // IL_00d8: mul
IL217: // IL_00d9: ldloc.s V_9
IL219: // IL_00db: ldfld System.Single Compute.Samples.Body::Mass
IL224: // IL_00e0: mul
IL225: // IL_00e1: ldloc.s V_13
IL227: // IL_00e3: div
IL228: // IL_00e4: stloc.s V_15
	local15 = (float) ((((((local9).Mass)) * (((((local1).Mass)) * (((float) 6.6743E-11f))))) / local13));
IL230: // IL_00e6: ldc.r4 1
IL235: // IL_00eb: ldloc.s V_14
IL237: // IL_00ed: div
IL238: // IL_00ee: stloc.s V_16
	local16 = (float) ((((float) 1.0f) / local14));
IL240: // IL_00f0: ldloc.2
IL241: // IL_00f1: ldloc.s V_10
IL243: // IL_00f3: ldloc.s V_16
IL245: // IL_00f5: mul
IL246: // IL_00f6: ldloc.s V_15
IL248: // IL_00f8: mul
IL249: // IL_00f9: add
IL250: // IL_00fa: stloc.2
	local2 = (float) ((((local15) * (((local16) * (local10)))) + local2));
IL251: // IL_00fb: ldloc.3
IL252: // IL_00fc: ldloc.s V_11
IL254: // IL_00fe: ldloc.s V_16
IL256: // IL_0100: mul
IL257: // IL_0101: ldloc.s V_15
IL259: // IL_0103: mul
IL260: // IL_0104: add
IL261: // IL_0105: stloc.3
	local3 = (float) ((((local15) * (((local16) * (local11)))) + local3));
IL262: // IL_0106: ldloc.s V_4
IL264: // IL_0108: ldloc.s V_12
IL266: // IL_010a: ldloc.s V_16
IL268: // IL_010c: mul
IL269: // IL_010d: ldloc.s V_15
IL271: // IL_010f: mul
IL272: // IL_0110: add
IL273: // IL_0111: stloc.s V_4
	local4 = (float) ((((local15) * (((local16) * (local12)))) + local4));
IL275: // IL_0113: nop
IL276: // IL_0114: ldloc.s V_8
IL278: // IL_0116: ldc.i4.1
IL279: // IL_0117: add
IL280: // IL_0118: stloc.s V_8
	local8 = (int) ((1 + local8));
IL282: // IL_011a: ldloc.s V_8
IL284: // IL_011c: ldloc.s V_5
IL286: // IL_011e: clt
IL288: // IL_0120: stloc.s V_18
	local18 = (int) ((((int) local5) > ((int) local8)));
IL290: // IL_0122: ldloc.s V_18
IL292: // IL_0124: brtrue IL_0042
	if (((int) local18) != 0) goto IL66;
IL297: // IL_0129: ldc.r4 1
IL302: // IL_012e: ldloc.1
IL303: // IL_012f: ldfld System.Single Compute.Samples.Body::Mass
IL308: // IL_0134: div
IL309: // IL_0135: stloc.s V_6
	local6 = (float) ((((float) 1.0f) / ((local1).Mass)));
IL311: // IL_0137: ldloca.s V_1
IL313: // IL_0139: ldflda Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL318: // IL_013e: ldloc.1
IL319: // IL_013f: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL324: // IL_0144: ldfld System.Single Compute.Samples.Vector3::X
IL329: // IL_0149: ldloc.2
IL330: // IL_014a: ldloc.s V_6
IL332: // IL_014c: mul
IL333: // IL_014d: ldarg.3
IL334: // IL_014e: mul
IL335: // IL_014f: add
IL336: // IL_0150: stfld System.Single Compute.Samples.Vector3::X
	(&((&(local1))->Velocity))->X = (((deltaTime) * (((local6) * (local2)))) + ((((local1).Velocity)).X));
IL341: // IL_0155: ldloca.s V_1
IL343: // IL_0157: ldflda Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL348: // IL_015c: ldloc.1
IL349: // IL_015d: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL354: // IL_0162: ldfld System.Single Compute.Samples.Vector3::Y
IL359: // IL_0167: ldloc.3
IL360: // IL_0168: ldloc.s V_6
IL362: // IL_016a: mul
IL363: // IL_016b: ldarg.3
IL364: // IL_016c: mul
IL365: // IL_016d: add
IL366: // IL_016e: stfld System.Single Compute.Samples.Vector3::Y
	(&((&(local1))->Velocity))->Y = (((deltaTime) * (((local6) * (local3)))) + ((((local1).Velocity)).Y));
IL371: // IL_0173: ldloca.s V_1
IL373: // IL_0175: ldflda Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL378: // IL_017a: ldloc.1
IL379: // IL_017b: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL384: // IL_0180: ldfld System.Single Compute.Samples.Vector3::Z
IL389: // IL_0185: ldloc.s V_4
IL391: // IL_0187: ldloc.s V_6
IL393: // IL_0189: mul
IL394: // IL_018a: ldarg.3
IL395: // IL_018b: mul
IL396: // IL_018c: add
IL397: // IL_018d: stfld System.Single Compute.Samples.Vector3::Z
	(&((&(local1))->Velocity))->Z = (((deltaTime) * (((local6) * (local4)))) + ((((local1).Velocity)).Z));
IL402: // IL_0192: ldloca.s V_1
IL404: // IL_0194: ldflda Compute.Samples.Vector3 Compute.Samples.Body::Position
IL409: // IL_0199: ldloc.1
IL410: // IL_019a: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL415: // IL_019f: ldfld System.Single Compute.Samples.Vector3::X
IL420: // IL_01a4: ldloc.1
IL421: // IL_01a5: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL426: // IL_01aa: ldfld System.Single Compute.Samples.Vector3::X
IL431: // IL_01af: ldarg.3
IL432: // IL_01b0: mul
IL433: // IL_01b1: add
IL434: // IL_01b2: stfld System.Single Compute.Samples.Vector3::X
	(&((&(local1))->Position))->X = (((deltaTime) * (((((local1).Velocity)).X))) + ((((local1).Position)).X));
IL439: // IL_01b7: ldloca.s V_1
IL441: // IL_01b9: ldflda Compute.Samples.Vector3 Compute.Samples.Body::Position
IL446: // IL_01be: ldloc.1
IL447: // IL_01bf: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL452: // IL_01c4: ldfld System.Single Compute.Samples.Vector3::Y
IL457: // IL_01c9: ldloc.1
IL458: // IL_01ca: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL463: // IL_01cf: ldfld System.Single Compute.Samples.Vector3::Y
IL468: // IL_01d4: ldarg.3
IL469: // IL_01d5: mul
IL470: // IL_01d6: add
IL471: // IL_01d7: stfld System.Single Compute.Samples.Vector3::Y
	(&((&(local1))->Position))->Y = (((deltaTime) * (((((local1).Velocity)).Y))) + ((((local1).Position)).Y));
IL476: // IL_01dc: ldloca.s V_1
IL478: // IL_01de: ldflda Compute.Samples.Vector3 Compute.Samples.Body::Position
IL483: // IL_01e3: ldloc.1
IL484: // IL_01e4: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Position
IL489: // IL_01e9: ldfld System.Single Compute.Samples.Vector3::Z
IL494: // IL_01ee: ldloc.1
IL495: // IL_01ef: ldfld Compute.Samples.Vector3 Compute.Samples.Body::Velocity
IL500: // IL_01f4: ldfld System.Single Compute.Samples.Vector3::Z
IL505: // IL_01f9: ldarg.3
IL506: // IL_01fa: mul
IL507: // IL_01fb: add
IL508: // IL_01fc: stfld System.Single Compute.Samples.Vector3::Z
	(&((&(local1))->Position))->Z = (((deltaTime) * (((((local1).Velocity)).Z))) + ((((local1).Position)).Z));
IL513: // IL_0201: ldarg.1
IL514: // IL_0202: ldloc.0
IL515: // IL_0203: ldloc.1
IL516: // IL_0204: stelem.any Compute.Samples.Body
	outBodies[local0] = (local1);
IL521: // IL_0209: ret
	return;


}



