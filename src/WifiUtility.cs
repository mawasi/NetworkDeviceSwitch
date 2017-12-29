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
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static public bool IsWifiApEnabled(Context context)
		{
			bool result = false;

			try {
				WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
				var method = wifiManager.Class.GetDeclaredMethod("isWifiApEnabled");
				result = (bool)method.Invoke(wifiManager);
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", $"IsWifiApEnabled() {e.ToString()}");
			}

			return result;
		}


		/// <summary>
		/// Get Wifi access point state.
		/// </summary>
		/// <param name="context"></param>
		/// <returns>WifiApState</returns>
		static public WifiApState GetWifiApState(Context context)
		{
			WifiApState state;

			try {
				WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
				var method = wifiManager.Class.GetDeclaredMethod("getWifiApState");
				state = (WifiApState)(int)method.Invoke(wifiManager);
				Android.Util.Log.Info("Info", $"WifiApState = {state}");
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", $"GetWifiApState() {e.ToString()}");
				state = WifiApState.Failed;
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
				var method = wifiManager.Class.GetDeclaredMethod("setWifiApEnabled", new WifiConfiguration().Class, Java.Lang.Boolean.Type);
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

		/// <summary>
		/// Toggle Wifi access point switch
		/// </summary>
		/// <remarks>
		/// WifiAp有効化前にWifi無効化処理を入れています.
		/// </remarks>
		/// <param name="context"></param>
		/// <param name="enabled"></param>
		/// <returns></returns>
		static public async Task<bool> ToggleWifiApAsync(Context context, bool enabled)
		{

			int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
			Android.Util.Log.Info("Info", $"WifiUtility.ToggleWifiApAsync({enabled}) ThreadID = {threadID}");

			// 待機時間
			const int WaitMilliSec = 100;

			// Enablingが完了したかチェック
			// todo: タイムアウト処理作る
			Func<Task<bool>> EnablingCheck = async () =>
			{
				while(true) {
					WifiApState value = GetWifiApState(context);
					if(value == WifiApState.Enabled) {
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
					WifiApState value = GetWifiApState(context);
					if(value == WifiApState.Disabled) {
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


			bool result = false;

			WifiApState wifiApState = GetWifiApState(context);

			if(enabled) {	// 有効化処理
				switch(wifiApState) {	// 現在のテザリングの状態によって処理を分ける
				case WifiApState.Enabled:
					result = true;
					break;
				case WifiApState.Disabled:
					// Set Wifi Disabled
					ToggleWifi(context, !enabled);

					// Waiting for Wifi Disabled.
					await Task.Run(() => WaitForWifiEnabled(!enabled));

					// Set WifiAp Enabled
					if(SetWifiApEnabled(context, enabled) == false) {
						break;	// 処理に失敗してた場合はここで処理終了
					}

					// テザリングの有効化チェック
					result = await EnablingCheck();

					break;
				case WifiApState.Failed:
				default:
					Android.Util.Log.Error("Error", "Unprocessed WifiAPState Switch. WifiAPState = {0}", wifiApState);
					break;
				}
			}
			else {	// 無効化処理
				switch(wifiApState) {
				case WifiApState.Enabled:
					// Set WifiAp Disabled
					if(SetWifiApEnabled(context, enabled) == false) {
						break;	// 処理に失敗してた場合はここで処理終了
					}

					// テザリングの無効化チェック
					result = await DisablingCheck(enabled);

					break;
				case WifiApState.Disabled:
					result = true;
					break;
				case WifiApState.Failed:
				default:
					Android.Util.Log.Error("Error", "Unprocessed WifiAPState Switch. WifiAPState = {0}", wifiApState);
					break;
				}
			}

			// WifiApの状態変化をブロードキャスト
			if(result) {
				Intent WifiApIntent = new Intent();
				WifiApIntent.SetAction(MainActivity.WIFI_AP_STATE_CHANGE);
				context.SendBroadcast(WifiApIntent);
			}

			return result;
		}


		#endregion // TetheringMethod

		#endregion // Method

	}
}