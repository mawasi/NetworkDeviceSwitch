

namespace NetworkDeviceSwitch
{
	/// <summary>
	/// テザリング機能の状態
	/// </summary>
	/// <remarks>
	/// https://github.com/android/platform_frameworks_base/blob/master/wifi/java/android/net/wifi/WifiManager.java
	/// </remarks>
	public enum WifiApState
	{
		/// <summary>
		/// Wi-Fi AP is currently being disabled. The state will change to [WifiAPState.Disabled] if it finishes successfully.
		/// </summary>
		Disabling = 10,
		/// <summary>
		/// Wi-Fi AP is disabled.
		/// </summary>
		Disabled = 11,	// 無効状態
		/// <summary>
		///  Wi-Fi AP is currently being enabled. The state will change to [WifiAPState.Enabled] if it finishes successfully.
		/// </summary>
		Enabling = 12,
		/// <summary>
		/// Wi-Fi AP is enabled.
		/// </summary>
		Enabled = 13,	// 有効状態
		/// <summary>
		/// Wi-Fi AP is in a failed state. This state will occur when an error occurs during enabling or disabling.
		/// </summary>
		Failed = 14
	}
}
