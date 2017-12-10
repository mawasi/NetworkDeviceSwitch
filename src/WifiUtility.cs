using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	}
}