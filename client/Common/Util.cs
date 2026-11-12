using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
	public class Util
	{
		public static int GetInt(object obj)
		{
			int res = 0;
			try
			{
				res = Convert.ToInt32(obj);
			}
			catch (Exception)
			{ }
			return res;
		}

	}
}
