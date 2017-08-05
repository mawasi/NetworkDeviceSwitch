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
using Android.Net.Wifi;

namespace NetworkDeviceSwitch
{
	/// <summary>
	/// Wifiデバイスモデルのコントローラ
	/// </summary>
	class WifiController
	{

		#region Definition

		/// <summary>
		/// テザリング機能の状態
		/// </summary>
		/// <remarks>
		/// ZenPad 3 8.0(Android6.0)では以下の値が返ってきた。
		/// 機種(OSVersion?)によっては違う値が帰ってくる可能性がある。
		/// </remarks>
		enum WifiAPState {
			Disabled = 11,	// 無効状態
			Enabled = 13,	// 有効状態
		}

		#endregion	// Definition


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
		/// Wifi Switch View
		/// </summary>
		Switch			_WifiSwitch = null;

		/// <summary>
		/// Tethering Switch View
		/// </summary>
		Switch			_TetheringSwitch = null;

		#endregion	// Field


		#region Property

		

		#endregion // Property

		#region BaseMethod

		/// <summary>
		/// Default Constructor
		/// </summary>
		private WifiController() {}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="activity"></param>
		/// <param name="wifi"></param>
		/// <param name="wifiSwitch"></param>
		/// <param name="tetheringSwitch"></param>
		public WifiController(Activity activity, WifiManager wifi, Switch wifiSwitch, Switch tetheringSwitch)
		{
			_Application = activity.Application;

			_WifiManager = wifi;

			_WifiSwitch = wifiSwitch;
			_WifiSwitch.CheckedChange += OnWifiSwitchCheckedChange;

			_TetheringSwitch = tetheringSwitch;
			_TetheringSwitch.CheckedChange += OnTetheringSwitchCheckedChange;
		}

		#endregion	// BaseMethod



		#region Method

		/// <summary>
		/// 各種初期化
		/// </summary>
		public void Initialize()
		{
			// Wifi機能が有効ならスイッチをONにしておく
			if(_WifiManager.IsWifiEnabled) {
				_WifiSwitch.Checked = true;
			}
			// Tethering機能が有効ならスイッチをON. Wifi機能とは排他的関係
			if(GetWifiApState()) {
				_TetheringSwitch.Checked = true;
			}
		}


		#region WifiMethod

		/// <summary>
		/// Called when the Wifi switch changes
		/// reference
		/// http://blog.dtdweb.com/2013/03/08/android-wifi-network/
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnWifiSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if(ToggleWifi(e.IsChecked)) {
				var message = e.IsChecked == true ? "Wifi Enabled." : "Wifi Disabled.";
				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}
		}


		/// <summary>
		/// Toggle Wifi switch
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		bool ToggleWifi(bool enabled)
		{
			bool result = false;
			if(_WifiManager.IsWifiEnabled != enabled) {
				_WifiManager.SetWifiEnabled(enabled);
				result = true;
			}

			return result;
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
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnTetheringSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{

#if false
			GetWifiApState();

			// リフレクションでメソッド全取得
			var methods = mWifiManager.Class.GetDeclaredMethods();

			Android.Util.Log.Info("TetheringTest", "mWifiManager Method List Start.");
			foreach(var method in methods) {
				Android.Util.Log.Info("TetheringTest", "Method: {0}", method.Name);
				foreach(var param in method.GetParameterTypes()) {
					Android.Util.Log.Info("TetheringTest", "Parameter: {0}", param.Name);
				}
			}
			Android.Util.Log.Info("TetheringTest", "mWifiManager Method List End.");


			Android.Util.Log.Info("TetheringTest", "dhcp Info {0}", mWifiManager.DhcpInfo.ToString());
			// WifiConfigurationにテザリング用のWifiコンフィグも入ってるみたい。
			var Configurations =  mWifiManager.ConfiguredNetworks;
			if(Configurations != null) {
				Android.Util.Log.Info("TetheringTest", "WifiConfigure Count {0}", Configurations.Count);
				for(int i = 0; i < Configurations.Count; i++) {
					Android.Util.Log.Info("TetheringTest", "WifiConfigure {0}", i);
					Android.Util.Log.Info("TetheringTest", "		ssid {0}", Configurations[i].Ssid);
					Android.Util.Log.Info("TetheringTest", "		NetworkId {0}", Configurations[i].NetworkId);
					Android.Util.Log.Info("TetheringTest", "		PreSharedKey {0}", Configurations[i].PreSharedKey);
					Android.Util.Log.Info("TetheringTest", "WifiConfigure {0} End.", i);
				}
			}
#else
/*
todo:
テザリングオンにする前にWifiをオフにする必要がある。
Wifiがオフになったのを確認してからテザリングをオンにする。
非同期処理に対応する必要あり。
C#のAsync Await使う？
それともandroidのServiceクラス使う？

*/

			if(ToggleWifiAp(e.IsChecked)) {
				var message = e.IsChecked == true ? "WifiAp Enabled." : "WifiAp Disabled.";
				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}

#endif

		}

		/// <summary>
		/// Toggle Wifi access point switch
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		bool ToggleWifiAp(bool enabled)
		{
			bool result = false;

			if(GetWifiApState() != enabled) {
				SetWifiApEnabled(enabled);
				result = true;
			}

			return result;
		}

		/// <summary>
		/// Get Wifi access point state.
		/// </summary>
		/// <returns>Ap Enabled is return true</returns>
		bool GetWifiApState()
		{
			bool result = false;

			try {
				// 11 = テザリング無効, 13 = テザリング有効	多分
				var method = _WifiManager.Class.GetDeclaredMethod("getWifiApState");
				method.Accessible = true;
				int state = (int)method.Invoke(_WifiManager);
				if(state == (int)WifiAPState.Enabled) {
					result = true;
				}
				Android.Util.Log.Info("WifiApState = ", state.ToString());
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", e.ToString());
			}

			return result;
		}

		/// <summary>
		/// Set Wifi access point enabled.
		/// </summary>
		/// <param name="enabled"></param>
		void SetWifiApEnabled(bool enabled)
		{
			try {
				var method = _WifiManager.Class.GetDeclaredMethod("setWifiApEnabled", new WifiConfiguration().Class, Java.Lang.Boolean.Type);
				method.Accessible = true;
				method.Invoke(_WifiManager, null, enabled);
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", e.ToString());
			}
		}

		#endregion	// TetheringMethod

		#endregion // Method

	}
}