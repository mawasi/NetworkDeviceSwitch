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

		#endregion

		#region Property

		

		#endregion

		#region BaseFunction

		/// <summary>
		/// Default Constructor
		/// </summary>
		private WifiController() {}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="s"></param>
		/// <param name="wifi"></param>
		public WifiController(Activity activity, Switch s, WifiManager wifi)
		{
			_Application = activity.Application;

			_WifiManager = wifi;

			_WifiSwitch = s;

			_WifiSwitch.CheckedChange += OnWifiSwitchCheckedChange;
		}

		#endregion

		#region Fuuction

		/// <summary>
		/// 各種初期化
		/// </summary>
		public void Initialize()
		{
			// Wifi機能が有効ならスイッチをONにしておく
			if(_WifiManager.IsWifiEnabled) {
				_WifiSwitch.Checked = true;
			}
		}

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

		#endregion

	}
}