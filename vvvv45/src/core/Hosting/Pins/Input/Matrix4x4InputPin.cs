﻿using System;
using System.Runtime.InteropServices;
using SlimDX;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

namespace VVVV.Hosting.Pins.Input
{
    [ComVisible(false)]
	public class Matrix4x4InputPin : DiffPin<Matrix4x4>
	{
		protected ITransformIn FTransformIn;
		
		public Matrix4x4InputPin(IPluginHost host, InputAttribute attribute)
			: base(host, attribute)
		{
			host.CreateTransformInput(FName, FSliceMode, FVisibility, out FTransformIn);
			base.InitializeInternalPin(FTransformIn);
		}
		
		protected override bool IsInternalPinChanged
		{
			get
			{
				return FTransformIn.PinIsChanged;
			}
		}
		
		unsafe protected static void CopyToBuffer(Matrix4x4[] buffer, float* source, int length)
		{
			fixed (Matrix4x4* destination = buffer)
			{
				Matrix4x4* dst = destination;
				Matrix* src = (Matrix*) source;
				
				for (int i = 0; i < length; i++)
					*(dst++) = (*(src++)).ToMatrix4x4();
			}
		}
		
		unsafe protected override void DoUpdate()
		{
			int length;
			float* source;
			
			FTransformIn.GetMatrixPointer(out length, out source);
			SliceCount = length;
			
			if (FSliceCount > 0)
				CopyToBuffer(FBuffer, source, length);
		}
	}
}
