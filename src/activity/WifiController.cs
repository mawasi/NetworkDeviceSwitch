﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.OS;
using Android.Content;
using Android.Net;
using Android.Widget;
using Android.Net.Wifi;

namespace NetworkDeviceSwitch
{
	/// <summary>
	/// Wifiデバイスモデルのコントローラ
	/// </summary>
	class WifiController : BroadcastReceiver
	{

		#region Field

		/// <summary>
		/// Application
		/// </summary>
		Application		_Application = null;

		/// <summary>
		/// WifiManager Model
		/// </summary>
		WifiManager		_WifiManager = null;

		/// <summary>
		/// ConnectivityManager
		/// </summary>
		ConnectivityManager _ConnectivityManager = null;

		/// <summary>
		/// Wifi Switch View
		/// </summary>
		Switch			_WifiSwitch = null;

		/// <summary>
		/// Tethering Switch View
		/// </summary>
		Switch			_TetheringSwitch = null;

		/// <summary>
		/// Wifi Status View
		/// </summary>
		TextView		_StatusView = null;

		/// <summary>
		/// WifiAp機能が利用可能かどうか
		/// </summary>
		bool			_IsWifiApActive = true;

		#endregion   // Field


		#region Method

		/// <summary>
		/// Default Constructor
		/// </summary>
		private WifiController() {}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="activity"></param>
		public WifiController(Activity activity)
		{
			_Application = activity.Application;

			_WifiManager = (WifiManager)activity.GetSystemService(Context.WifiService);

			_ConnectivityManager = (ConnectivityManager)activity.GetSystemService(Context.ConnectivityService);

			_WifiSwitch = activity.FindViewById<Switch>(Resource.Id.WifiSwitch);
			_WifiSwitch.CheckedChange += OnWifiSwitchCheckedChange;

			_TetheringSwitch = activity.FindViewById<Switch>(Resource.Id.TetheringSwitch);
			_TetheringSwitch.CheckedChange += OnTetheringSwitchCheckedChange;

			_StatusView = activity.FindViewById<TextView>(Resource.Id.StatusView);
			_StatusView.Text = "";
		}

		/// <summary>
		/// 各種ビューの初期化
		/// </summary>
		public void Initialize(Context context)
		{
			_StatusView.Text = "";

			// Wifi機能が有効ならスイッチをONにしておく
			if(_WifiManager.IsWifiEnabled) {
				_WifiSwitch.Checked = true;
			}
			// Tethering機能が有効ならスイッチをON. Wifi機能とは排他的関係
			WifiApState wifiApState = WifiUtility.GetWifiApState(_Application);
			_TetheringSwitch.Enabled = true;	// 一旦有効にしておく
			if (wifiApState == WifiApState.Enabled) {
				_TetheringSwitch.Checked = true;
			}
			else {
				if(wifiApState == WifiApState.Failed) {
					// ステートが失敗ならviewを無効化
					_TetheringSwitch.Enabled = false;
					_IsWifiApActive = false;
				}
				else {
					_TetheringSwitch.Checked = false;
				}
			}

			CheckNetworkState(context);
		}

		/// <summary>
		/// ブロードキャスト受け取り
		/// </summary>
		/// <param name="context"></param>
		/// <param name="intent"></param>
		public override void OnReceive(Context context, Intent intent)
		{
			DebugUtility.LogInfo("Info", $"Wifi 関連のブロードキャストを受け取りました. Action = {intent.Action} ");
			// 別のアプリからフォーカス戻したときも以下のアクションでレシーブされる。
			if(intent.Action == MainActivity.SCAN_RESULTS) {
				// 最寄りの登録済みAPに接続を試みる
				WifiUtility.TryConnectAP(context);
			}

			if(intent.Action == MainActivity.WIFI_STATE_CHANGE){
				DebugUtility.LogInfo("Info", $"IsWifiEnabled = {_WifiManager.IsWifiEnabled} : WifiState = {_WifiManager.WifiState.ToString()} : WifiSwitch = {_WifiSwitch.Checked} ");
				// Wifiデバイスの状態とスイッチの状態が一致してない場合に、デバイスの状態にスイッチを合わせる
				if((_WifiManager.WifiState == WifiState.Disabled)
					|| (_WifiManager.WifiState == WifiState.Enabled)){
					if(_WifiSwitch.Checked != _WifiManager.IsWifiEnabled){
						_WifiSwitch.Checked = _WifiManager.IsWifiEnabled;
					}
					// ToastはUIスレッド以外で呼び出せない
					var message = $"Wifi {_WifiManager.WifiState}.";
					Toast.MakeText(_Application, message, ToastLength.Short).Show();
				}
			}

			if(intent.Action == MainActivity.WIFI_AP_STATE_CHANGE) {
				// WifiApデバイスの状態とスイッチの状態が一致してない場合に、デバイスの状態にスイッチを合わせる
				// Wifi版と違って独自に作ったActionなのでStateのチェックはしない
				bool wifiApEnabled = WifiUtility.IsWifiApEnabled(context);
				if(_TetheringSwitch.Checked != wifiApEnabled) {
					_TetheringSwitch.Checked = wifiApEnabled;
				}
			}


			if(intent.Action == MainActivity.CONNECTIVITY_CHANGE){
				CheckNetworkState(context);
			}

		}

		/// <summary>
		/// Enable switch view.
		/// </summary>
		/// <param name="enabled"></param>
		public void EnableSwitchView(bool enabled)
		{
			_WifiSwitch.Enabled = enabled;
			if(_IsWifiApActive) {
				_TetheringSwitch.Enabled = enabled;
			}
		}

		/// <summary>
		/// 通信情報確認
		/// </summary>
		void CheckNetworkState(Context context)
		{

			bool isOnline = WifiUtility.IsOnline(_Application);


			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("SDK Build Version : {0}\n", Build.VERSION.Sdk);
			builder.AppendFormat("NetworkState : {0}\n", (isOnline ? "Online" : "Offline"));

			if (isOnline)
			{
				NetworkInfo activeNetworkInfo = _ConnectivityManager.ActiveNetworkInfo;
				builder.Append($"ConnectType : {activeNetworkInfo.TypeName}\n");
/*
				List<NetworkInfo> mobileinfos = WifiUtility.GetNetworkInfo(context);
				for(int i = 0; i < mobileinfos.Count; i++){
					builder.Append($"Mobile[{i}] IsAvailable : {mobileinfos[i].IsAvailable}\n");
					builder.Append($"Mobile[{i}] IsConnected : {mobileinfos[i].IsConnectedOrConnecting}\n");
				}
*/

				switch (activeNetworkInfo.Type)
				{
					case ConnectivityType.Wifi:
						WifiInfo info = _WifiManager.ConnectionInfo;
						builder.AppendFormat("BSSID : {0}\n", info.BSSID);
						builder.AppendFormat("SSID : {0}\n", info.SSID);

						byte[] byteArray = BitConverter.GetBytes(info.IpAddress);
						Java.Net.InetAddress inetAddress = Java.Net.InetAddress.GetByAddress(byteArray);
						string ipaddress = inetAddress.HostAddress;
						builder.AppendFormat("IpAddress : {0}\n", ipaddress);
						break;
					case ConnectivityType.Mobile:
						break;
					default: break;
				}
			}

			var telephony = (Android.Telephony.TelephonyManager)context.GetSystemService(Context.TelephonyService);
			if (telephony != null)
			{
				builder.Append($"SIM State : {telephony.SimState}\n");
			}

			_StatusView.Text = builder.ToString();
		}

		#region WifiMethod

		/// <summary>
		/// Called when the Wifi switch changes
		/// reference
		/// http://blog.dtdweb.com/2013/03/08/android-wifi-network/
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void OnWifiSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			EnableSwitchView(false);

			await Task.Run(() => WifiUtility.ToggleWifiAsync(_Application, e.IsChecked));

			EnableSwitchView(true);
		}



		#endregion // WifiMethod


		#region TetheringMethod

		/// <summary>
		/// Called when the Tethering switch changes
		/// required permission Manifest.permission.WRITE_SETTINGS
		/// reference
		/// http://qiita.com/ki_siro/items/a45c27ee3cb204487b85
		/// https://sites.google.com/site/umibenojinjin/home/android
		/// http://stackoverflow.com/questions/7048922/android-2-3-wifi-hotspot-api
		/// </summary>
		/// <remarks>
		/// テザリングONにする際に、事前にWifiをOFFにしておかないと失敗する.
		/// WifiがOFFになったのを確認してからテザリングをONにする.
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void OnTetheringSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
			DebugUtility.LogInfo("Info", $"WifiController.OnTetheringSwitchCheckedChange({e.IsChecked}) ThreadID = {threadID}");
#if false
			GetWifiApState();

			// リフレクションでメソッド全取得
			var methods = mWifiManager.Class.GetDeclaredMethods();

			DebugUtility.LogInfo("TetheringTest", "mWifiManager Method List Start.");
			foreach(var method in methods) {
				DebugUtility.LogInfo("TetheringTest", $"Method: {method.Name}");
				foreach(var param in method.GetParameterTypes()) {
					DebugUtility.LogInfo("TetheringTest", $"Parameter: {param.Name}");
				}
			}
			DebugUtility.LogInfo("TetheringTest", "mWifiManager Method List End.");


			DebugUtility.LogInfo("TetheringTest", $"dhcp Info {0}", mWifiManager.DhcpInfo.ToString());
			// WifiConfigurationにテザリング用のWifiコンフィグも入ってるみたい。
			var Configurations =  mWifiManager.ConfiguredNetworks;
			if(Configurations != null) {
				DebugUtility.LogInfo("TetheringTest", "WifiConfigure Count {0}", Configurations.Count);
				for(int i = 0; i < Configurations.Count; i++) {
					DebugUtility.LogInfo("TetheringTest", "WifiConfigure {0}", i);
					DebugUtility.LogInfo("TetheringTest", "		ssid {0}", Configurations[i].Ssid);
					DebugUtility.LogInfo("TetheringTest", "		NetworkId {0}", Configurations[i].NetworkId);
					DebugUtility.LogInfo("TetheringTest", "		PreSharedKey {0}", Configurations[i].PreSharedKey);
					DebugUtility.LogInfo("TetheringTest", "WifiConfigure {0} End.", i);
				}
			}
#else

			WifiApState apstate = WifiUtility.GetWifiApState(_Application);
			if(apstate == WifiApState.Failed) {
				var message = "This Device is not supported WifiAP.";
				Toast.MakeText(_Application, message, ToastLength.Short).Show();
				return;
			}

			// 処理終わるまで再度スイッチ押せないように無効化する
			EnableSwitchView(false);

			// テザリング機能の有効無効を切り替える
			bool result = await Task.Run(() => WifiUtility.ToggleWifiApAsync(_Application, e.IsChecked));

			// 処理が終わったら有効化
			EnableSwitchView(true);

			if (result) {
				var message = e.IsChecked == true ? "WifiAp Enabled." : "WifiAp Disabled.";

				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}
			else {	// WifiAP 有効,無効化失敗時
				var message = e.IsChecked == true ? "WifiAp Enabling is fail." : "WifiAp Disabling is fail.";
	//			_TetheringSwitch.Checked = e.IsChecked == true ? false : true;

				Toast.MakeText(_Application, message, ToastLength.Short).Show();

				// スイッチの表示とデバイスの状態が一致してなかったら、デバイスの状態にスイッチを合わせておく
				bool wifiApEnabled = WifiUtility.IsWifiApEnabled(_Application);
				if(e.IsChecked != wifiApEnabled){
					_TetheringSwitch.Checked = wifiApEnabled;
				}
			}
#endif

		}


		#endregion // TetheringMethod

		#endregion // Method

	}
}