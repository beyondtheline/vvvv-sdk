﻿using System;
using System.Runtime.InteropServices;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Hosting.Pins.Input
{
    [ComVisible(false)]
	public class DiffInputSpreadList<T> : DiffSpreadList<T>
	{
		public DiffInputSpreadList(IPluginHost host, InputAttribute attribute)
			: base(host, attribute)
		{
		}
		
		//create a pin at position
		protected override IDiffSpread<T> CreatePin(int pos)
		{
			//create pin name
			var origName = FAttribute.Name;
			FAttribute.Name = origName + " " + pos;
			
			var ret	= PinFactory.CreateDiffPin<T>(FHost, FAttribute as InputAttribute);
			ret.PluginIO.Order = FOffsetCounter * 1000 + pos;
			ret.Updated += Pin_Updated;
			
			//set attribute name back
			FAttribute.Name = origName;
			
			return ret;
		}
		
	}
}
