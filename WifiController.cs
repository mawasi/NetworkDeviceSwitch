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
			if(GetWifiApState() == WifiAPState.Enabled) {
				_TetheringSwitch.Checked = true;
			}
			else {
				_TetheringSwitch.Checked = false;
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

		/// <summary>
		/// Toggle Wifi and Check Wifi Enabled.
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		bool ToggleWifiAsync(bool enabled)
		{
			bool result = false;

			result = ToggleWifi(enabled);

			while(true) {
				if(_WifiManager.IsWifiEnabled == enabled) {
					return result;
				}
			}
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
		/// テザリングオンにする前にWifiをオフにする必要がある。
		/// Wifiがオフになったのを確認してからテザリングをオンにする。
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void OnTetheringSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
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

			int apstate = GetWifiApState();
			if(apstate == WifiAPState.Failed) {
				var message = "This Device is not support WifiAP.";
				Toast.MakeText(_Application, message, ToastLength.Short).Show();
				return;
			}

			bool result = await Task.Run(() => ToggleWifiApAsync(e.IsChecked));
			if(result) {
				var message = e.IsChecked == true ? "WifiAp Enabled." : "WifiAp Disabled.";
				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}
			else {
				var message = e.IsChecked == true ? "WifiAp Enabling is fail." : "WifiAp Disabling is fail.";
				_TetheringSwitch.Checked = e.IsChecked == true ? false : true;

				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}

#endif

		}

		/// <summary>
		/// Toggle Wifi access point switch
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		async Task<bool> ToggleWifiApAsync(bool enabled)
		{
			bool result = false;

			// Enablingが完了したかチェック
			Func<int> EnablingCheck = () =>
			{
				while(true) {
					int value = GetWifiApState();
					if(value != WifiAPState.Enabling) {
						return value;
					}
				}
			};

			// Disablingが完了したかチェック
			Func<int> DisablingCheck = () =>
			{
				while(true) {
					int value = GetWifiApState();
					if(value != WifiAPState.Disabling) {
						return value;
					}
				}
			};


			if(enabled) {				// 有効化処理
				int ApState =  GetWifiApState();
				switch(ApState) {	// 現在のテザリングの状態によって処理を分ける
				case WifiAPState.Enabled:
					result = true;
					break;
				case WifiAPState.Disabled:
					// Switch Wifi Disabled
					await Task.Run(() => ToggleWifiAsync(!enabled));

					// Set WifiAp
					SetWifiApEnabled(enabled);

					// テザリングの有効化チェック
					int state = await Task.Run(EnablingCheck);
					if(state == WifiAPState.Enabled) {
						result = true;
					}

					break;
				case WifiAPState.Failed:
					break;
				default:
					Android.Util.Log.Error("Error", "Unprocessed WifiAPState Switch. WifiAPState = " + ApState.ToString());
					break;
				}

			}
			else {				// 無効化処理
				int ApState =  GetWifiApState();
				switch(ApState) {
				case WifiAPState.Enabled:
					// Set WifiAp
					SetWifiApEnabled(enabled);

					// テザリングの有効化チェック
					int state = await Task.Run(DisablingCheck);
					if(state == WifiAPState.Disabled) {
						result = true;
					}

					// Switch Wifi Enabled
					await Task.Run(() => ToggleWifiAsync(!enabled));

					break;
				case WifiAPState.Failed:
					break;
				case WifiAPState.Disabled:
					result = true;
					break;
				default:
					Android.Util.Log.Error("Error", "Unprocessed WifiAPState Switch. WifiAPState = " + ApState.ToString());
					break;
				}
			}

#if false
			if(GetWifiApState() != enabled) {


				await Task.Run(() => {
										// Switch Wifi Enabled
										ToggleWifi(!enabled);
										// Wait for changes to be reflected
										while(true) {
											if(_WifiManager.IsWifiEnabled == (!enabled)) {
												return;
											}
										}
									});

				SetWifiApEnabled(enabled);
				result = true;
			}
#endif

			return result;
		}

		/// <summary>
		/// Get Wifi access point state.
		/// </summary>
		/// <returns>return AP state.</returns>
		int GetWifiApState()
		{
			int state;

			try {
				var method = _WifiManager.Class.GetDeclaredMethod("getWifiApState");
				method.Accessible = true;
				state = (int)method.Invoke(_WifiManager);
				Android.Util.Log.Info("WifiApState = ", state.ToString());
			}
			catch(Exception e) {
				Android.Util.Log.Error("Error", e.ToString());
			}
			finally {
				// Treat as Fail.
				state = WifiAPState.Failed;
			}

			return state;
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

		#endregion // TetheringMethod

		#endregion // Method

	}
}