using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace NetworkDeviceSwitch
{
	/// <summary>
	/// デバッグ用ユーティリティ
	/// </summary>
	class DebugUtility
	{
		/// <summary>
		/// Send an Android.Util.LogPriority.Info log message.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="msg"></param>
		[Conditional("DEBUG")]
		static public void LogInfo(string tag, string msg)
		{
			Android.Util.Log.Info(tag, msg);
		}

		[Conditional("DEBUG")]
		static public void LogError(string tag, string msg)
		{
			Android.Util.Log.Error(tag, msg);
		}
	}
}