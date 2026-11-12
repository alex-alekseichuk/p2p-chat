using System;
using System.Threading;

namespace Common
{
	public class ThreadEx
	{

		#region LoopSleep

		public static void LoopSleep(ref int loopIndex)
		{

			if (Environment.ProcessorCount == 1 || (++loopIndex % (Environment.ProcessorCount * 50)) == 0)
			{
				//----- Single-core!
				//----- Switch to another running thread!
				Thread.Sleep(5);
			}
			else
			{
				//----- Multi-core / HT!
				//----- Loop n iterations!
				Thread.SpinWait(20);
			}

		}

		#endregion

		#region SleepEx

		public static void SleepEx(int milliseconds)
		{
			Thread.Sleep(milliseconds <= 0 ? 1 : milliseconds);
		}

		#endregion

	}
}