using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Net;
using Android.Net.Wifi;

namespace NetworkDeviceSwitch
{
	/// <summary>
	/// Wifi関連の機能を操作する
	/// </summary>
	public class WifiUtility
	{
		#region Definition

		/// <summary>
		/// テザリング機能の状態
		/// </summary>
		/// <remarks>
		/// https://github.com/android/platform_frameworks_base/blob/master/wifi/java/android/net/wifi/WifiManager.java
		/// </remarks>
		public class WifiAPState
		{
			/// <summary>
			/// Wi-Fi AP is currently being disabled. The state will change to [WifiAPState.Disabled] if it finishes successfully.
			/// </summary>
			public const int Disabling = 10;
			/// <summary>
			/// Wi-Fi AP is disabled.
			/// </summary>
			public const int Disabled = 11;	// 無効状態
			/// <summary>
			///  Wi-Fi AP is currently being enabled. The state will change to [WifiAPState.Enabled] if it finishes successfully.
			/// </summary>
			public const int Enabling = 12;
			/// <summary>
			/// Wi-Fi AP is enabled.
			/// </summary>
			public const int Enabled = 13;	// 有効状態
			/// <summary>
			/// Wi-Fi AP is in a failed state. This state will occur when an error occurs during enabling or disabling.
			/// </summary>
			public const int Failed = 14;
		}

		#endregion // Definition


		#region Method

		/// <summary>
		/// Wifiなり、Moblieなり、何かしらのネットワークに接続されているかどうか
		/// Check connected to the network. (e.g. Wifi, Moblie)
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static public bool IsOnline(Context context)
		{
			ConnectivityManager	connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

			// 機内モード等で、いずれのネットワークも見つからなかった場合、 ActiveNetworkInfo は null を返す。
			NetworkInfo info = connectivityManager?.ActiveNetworkInfo;
		
			return info?.IsConnected ?? false;	
		}

		#region WifiMethod

		/// <summary>
		/// Wifi 有効,無効確認
		/// </summary>
		/// <param name="context"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		static public bool IsWifiEnabled(Context context, bool flag)
		{
			WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
			if(wifiManager.IsWifiEnabled == flag) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// Toggle Wifi Enabled
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static public bool ToggleWifi(Context context, bool enabled)
		{
			WifiManager	wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);

			if(wifiManager == null) return false;

			if(wifiManager.IsWifiEnabled != enabled){
				wifiManager.SetWifiEnabled(enabled);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Try connect to Access Point.
		/// </summary>
		/// <param name="context"></param>
		static public void TryConnectAP(Context context)
		{
			// すでに接続済みなら何もしない
			if(IsOnline(context)){
				Android.Util.Log.Info("Info", "Already connected.");
				return;
			}

			WifiManager	wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
			IList<ScanResult>	results = wifiManager.ScanResults;

			// APスキャン結果
			Android.Util.Log.Info("Info", "AP Candidate Scan Result Start.");
			foreach(var result in results){
				Android.Util.Log.Info("Info", "		ssid {0}", result.Ssid);
			}
			Android.Util.Log.Info("Info", "AP Candidate Scan Result End.");

			// 端末に保存されているネットワーク設定リスト
			var ConfiguredNetworks = wifiManager.ConfiguredNetworks;

			// スキャンしたAPのSSIDと設定リストに登録されてるSSIDで一致するものがあり
			// なおかつその中で一番Frequencyが高いやつに接続する
			WifiConfiguration candidacy = null;
			int frequency = 0;
			foreach(var config in ConfiguredNetworks){
				foreach(var result in results){
					var ssid = config.Ssid.Replace("\"", "");	// 接頭、接尾の["]が邪魔なので削除する
					if(ssid.Equals(result.Ssid)){
						candidacy = config;	// 接続候補
						frequency = result.Frequency;
					}
				}
			}

			if(candidacy != null){
				wifiManager.EnableNetwork(candidacy.NetworkId, true);
				Toast.MakeText(context, $"Wifi Connecting. SSID = {candidacy.Ssid}", ToastLength.Long).Show();
			}

		}

		#endregion // WifiMethod

		#region TetheringMethod



		/// <summary>
		/// Get Wifi access point state.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>WifiAPstate</returns>
		static public int GetWifiApState(Context context)
		{
			int state;

			try {
				WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
				var method = wifiManager.Class.GetDeclaredMethod("getWifiApState");
				state = (int)method.Invoke(wifiManager);
				Android.Util.Log.Info("Info", $"WifiApState = {state}");
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", e.ToString());
				state = WifiAPState.Failed;
			}

			return state;
		}


		/// <summary>
		/// Set Wifi access point enabled.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="enabled"></param>
		/// <returns></returns>
		static public bool SetWifiApEnabled(Context context, bool enabled)
		{
			bool result;
			try {
				WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
				var method = wifiManager.Class.GetDeclaredMethod("setWifiApEnabled");
				method.Accessible = true;
				method.Invoke(wifiManager, null, enabled);
				result = true;
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", e.ToString());
				result = false;
			}

			return result;
		}

#if false
		static public async Task<bool> ToggleWifiApAsync(Context context, bool enabled)
		{
			bool result = false;

			int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
			Android.Util.Log.Info("Info", $"WifiUtility.ToggleWifiApAsync({result}) ThreadID = {threadID}");

			// 待機時間
			const int WaitMilliSec = 100;

			// Enablingが完了したかチェック
			// todo: タイムアウト処理作る
			Func<Task<bool>> EnablingCheck = async () =>
			{
				while(true) {
					int value = WifiUtility.GetWifiApState(context);
					if(value == WifiUtility.WifiAPState.Enabled) {
						return true;
					}
					await Task.Delay(WaitMilliSec);	// 100ミリ待機
				}
			};

			// Disablingが完了したかチェック
			// todo: タイムアウト処理作る
			Func<bool, Task<bool>> DisablingCheck = async (flag) =>
			{
				while(true) {
					int value = WifiUtility.GetWifiApState(context);
					if(value == WifiUtility.WifiAPState.Disabled) {
						return true;
					}
					await Task.Delay(WaitMilliSec);	// 100ミリ待機
				}
			};

			// Wifi 有効(無効)化待ち
			// todo: タイムアウト処理作る
			Action<bool> WaitForWifiEnabled = async (flag) =>
			{
				while(true) {
					if(WifiUtility.IsWifiEnabled(context, flag)){
						return;
					}
					await Task.Delay(WaitMilliSec);
				}
			};

		}
#endif

		#endregion // TetheringMethod

		#endregion // Method

	}
}