﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Content;
using Android.Widget;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Telephony;

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

		/// <summary>
		/// Check Mobile data connection enabled.
		/// モバイルデータ通信が可能かどうか
		/// </summary>
		/// <returns></returns>
		static public bool IsMobileDataConnectionEnabled(Context context)
		{
			bool result = false;

			if(!IsOnline(context)) return result;
#if false	// ConnectivityManager使ってチェックするパターン。5.1以降うまく欲しい情報が取れない。
			List<NetworkInfo> infos = GetNetworkInfo(context);
			foreach(var info in infos){
				if(info.Type == ConnectivityType.Mobile){
					result = info.IsAvailable;
					break;
				}
			}
#else
			// SimState.Ready = モバイルデータ通信可能とみなす
			var TelephonyManager = (TelephonyManager)context.GetSystemService(Context.TelephonyService);
			if(TelephonyManager.SimState == SimState.Ready){
				result = true;
			}
#endif
			return result;
		}


		/// <summary>
		/// GetNetworkInfo wrapper
		/// </summary>
		/// <remarks>
		/// GetAllNetworks では、トラッキングされたネットワークしか取得されない。
		/// トラッキングされたがどういう意味かいまいちわからないけど、
		/// 挙動的には GetActiveNetwork と変わらない動きする。
		/// </remarks>
		/// <param name="type"></param>
		/// <returns></returns>
		static public List<NetworkInfo> GetNetworkInfo(Context context)
		{
			List<NetworkInfo> infos = new List<NetworkInfo>();

			var ConnectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

			// OS version が5.1以上向け
			if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1){
				var networks = ConnectivityManager.GetAllNetworks();
				foreach (var network in networks){
					var networkinfo = ConnectivityManager.GetNetworkInfo(network);
					infos.Add(networkinfo);
				}
			}
			else{
				// Lollipop以前の端末はこっちの処理
				NetworkInfo[] MobileInfo = ConnectivityManager.GetAllNetworkInfo();
				infos.AddRange(MobileInfo);
			}

			return infos;
		}



		#region WifiMethod

		/// <summary>
		/// Wifi 有効,無効確認
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static public bool IsWifiEnabled(Context context)
		{
			WifiManager wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
			return wifiManager?.IsWifiEnabled ?? false;
		}

		/// <summary>
		/// Toggle Wifi Enabled
		/// </summary>
		/// <remarks>
		/// 外部向け
		/// </remarks>
		/// <param name="context"></param>
		/// <returns></returns>
		static public async Task<bool> ToggleWifiAsync(Context context, bool enabled)
		{
			WifiManager	wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);

			if(wifiManager == null) return false;

			// Wifi有効化する際、WifiApがすでに有効な場合まず、WifiApを無効化する.
			if(enabled) {
				if(IsWifiApEnabled(context)) {
					await ToggleWifiApAsync(context, false);
				}
			}

			if(wifiManager.IsWifiEnabled != enabled){
				wifiManager.SetWifiEnabled(enabled);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Toggle Wifi Enabled
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static private bool ToggleWifi(Context context, bool enabled)
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
				DebugUtility.LogInfo("Info", "Already connected.");
				return;
			}

			WifiManager	wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
			IList<ScanResult>	results = wifiManager.ScanResults;

			// APスキャン結果
			DebugUtility.LogInfo("Info", "AP Candidate Scan Result Start.");
			foreach(var result in results){
				DebugUtility.LogInfo("Info", $"		ssid {result.Ssid}");
			}
			DebugUtility.LogInfo("Info", "AP Candidate Scan Result End.");

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
		/// Wifi AP が有効かどうか
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
				DebugUtility.LogError("Error", $"IsWifiApEnabled() {e.ToString()}");
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
				DebugUtility.LogInfo("Info", $"WifiApState = {state}");
			}
			catch(Exception e) {
				DebugUtility.LogError("Error", $"GetWifiApState() {e.ToString()}");
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
				DebugUtility.LogError("Error", e.ToString());
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
			DebugUtility.LogInfo("Info", $"WifiUtility.ToggleWifiApAsync({enabled}) ThreadID = {threadID}");

			// 待機時間
			const int WaitMilliSec = 100;
			// 試行回数100回まで
			const int MaxTrials = 100;


			// WifiAp 有効(無効)化待ち
			Func<bool, Task<bool>> WaitForWifiApEnabled = async (flag) =>
			{
				for(var i = 0; i < MaxTrials; i++){
					if(IsWifiApEnabled(context) == flag){
						return true;
					}
					await Task.Delay(WaitMilliSec);
				}
				return false;
			};

			// Wifi 有効(無効)化待ち
			Func<bool, Task<bool>> WaitForWifiEnabled = async (flag) =>
			{
				for(var i = 0; i < MaxTrials; i++) {
					if(IsWifiEnabled(context) == flag){
						return true;
					}
					await Task.Delay(WaitMilliSec);
				}
				return false;
			};


			// モバイルデータ通信ができない場合はテザリング機能ON,OFFを行わない。
			if(!IsMobileDataConnectionEnabled(context)){
				return false;
			}


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

					// テザリングの有効化待ち
					result = await WaitForWifiApEnabled(enabled);

					break;
				case WifiApState.Failed:
				default:
					DebugUtility.LogError("Error", $"Unprocessed WifiAPState Switch. WifiAPState = {wifiApState}");
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

					// テザリングの無効化待ち
					result = await WaitForWifiApEnabled(enabled);

					break;
				case WifiApState.Disabled:
					result = true;
					break;
				case WifiApState.Failed:
				default:
					DebugUtility.LogError("Error", $"Unprocessed WifiAPState Switch. WifiAPState = {wifiApState}");
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