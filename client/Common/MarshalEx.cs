using System;
using System.Runtime.InteropServices;

namespace Common
{

	public class MarshalEx
	{

		#region Methods

		public static object[] PtrToStructureArray(IntPtr pointer, Type structureType, int len)
		{

			object[] array = new object[len];

			for (int i = 0; i < len; i++)
			{
				array[i] = Marshal.PtrToStructure(pointer, structureType);
				pointer = (IntPtr)(pointer.ToInt32() + Marshal.SizeOf(array[i]));
			}

			return array;

		}

		#endregion

	}
}