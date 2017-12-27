using System;
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
		/// ブロードキャスト受け取り
		/// </summary>
		/// <param name="context"></param>
		/// <param name="intent"></param>
		public override void OnReceive(Context context, Intent intent)
		{
			Android.Util.Log.Info("Info", "Wifi 関連のブロードキャストを受け取りました. Action = {0} ", intent.Action);
			// 別のアプリからフォーカス戻したときも以下のアクションでレシーブされる。
			if(intent.Action == MainActivity.SCAN_RESULTS) {
				// 最寄りの登録済みAPに接続を試みる
				WifiUtility.TryConnectAP(context);
			}

			if(intent.Action == MainActivity.WIFI_STATE_CHANGE){
				Android.Util.Log.Info("Info", $"IsWifiEnabled = {_WifiManager.IsWifiEnabled} : WifiState = {_WifiManager.WifiState.ToString()} : WifiSwitch = {_WifiSwitch.Checked} ");
				// Wifiデバイスの状態とスイッチの状態が一致してない場合に、デバイスの状態にスイッチを合わせる
				if((_WifiManager.WifiState == WifiState.Disabled) || (_WifiManager.WifiState == WifiState.Enabled)){
					if(_WifiSwitch.Checked != _WifiManager.IsWifiEnabled){
						_WifiSwitch.Checked = _WifiManager.IsWifiEnabled;
					}
					// ToastはUIスレッド以外で呼び出せない
					var message = $"Wifi {_WifiManager.WifiState}.";
					Toast.MakeText(_Application, message, ToastLength.Short).Show();
				}
			}


			if(intent.Action == MainActivity.CONNECTIVITY_CHANGE){
				CheckNetworkState();
#if false
				// テザリングの状態がDisabledならスイッチの状態をOFFにする	
				if(GetWifiApState() == WifiAPState.Disabled){
					if(_TetheringSwitch.Checked){
						_TetheringSwitch.Checked = false;
					}
				}
#endif
			}

		}


		/// <summary>
		/// 各種ビューの初期化
		/// </summary>
		public void Initialize()
		{
			_StatusView.Text = "";

			// Wifi機能が有効ならスイッチをONにしておく
			if(_WifiManager.IsWifiEnabled) {
				_WifiSwitch.Checked = true;
			}
			// Tethering機能が有効ならスイッチをON. Wifi機能とは排他的関係
			int wifiApState = WifiUtility.GetWifiApState(_Application);
			_TetheringSwitch.Enabled = true;	// 一旦有効にしておく
			if (wifiApState == WifiUtility.WifiAPState.Enabled) {
				_TetheringSwitch.Checked = true;
			}
			else {
				if(wifiApState == WifiUtility.WifiAPState.Failed) {
					// ステートが失敗ならviewを無効化しておく
					_TetheringSwitch.Enabled = false;
				}
				else {
					_TetheringSwitch.Checked = false;
				}
			}
		}

		/// <summary>
		/// Enable switch view.
		/// </summary>
		/// <param name="enabled"></param>
		public void EnableSwitchView(bool enabled)
		{
			_WifiSwitch.Enabled = enabled;
			_TetheringSwitch.Enabled = enabled;
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
			EnableSwitchView(false);

			WifiUtility.ToggleWifi(_Application, e.IsChecked);

			EnableSwitchView(true);
		}

		/// <summary>
		/// 通信情報確認
		/// </summary>
		void CheckNetworkState()
		{

			bool isOnline = WifiUtility.IsOnline(_Application);


			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("SDK Build Version : {0}\n", Build.VERSION.Sdk);
			builder.AppendFormat("NetworkState : {0}\n", (isOnline ? "Online" : "Offline"));

			if(isOnline) {
				NetworkInfo activeNetworkInfo = _ConnectivityManager.ActiveNetworkInfo;
				builder.AppendFormat("ConnectType : {0}\n", activeNetworkInfo.TypeName);

				switch(activeNetworkInfo.Type) {
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

			_StatusView.Text = builder.ToString();
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
			int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
			Android.Util.Log.Info("Info", $"WifiController.OnTetheringSwitchCheckedChange({e.IsChecked}) ThreadID = {threadID}");
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

			int apstate = WifiUtility.GetWifiApState(_Application);
			if(apstate == WifiUtility.WifiAPState.Failed) {
				var message = "This Device is not supported WifiAP.";
				Toast.MakeText(_Application, message, ToastLength.Short).Show();
				return;
			}

			// 処理終わるまで再度スイッチ押せないように無効化する
			EnableSwitchView(false);

			// テザリング機能の有効無効を切り替える
			bool result = await Task.Run(() => ToggleWifiApAsync(e.IsChecked));

			// 処理が終わったら有効化
			EnableSwitchView(true);

			if (result) {
				var message = e.IsChecked == true ? "WifiAp Enabled." : "WifiAp Disabled.";

				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}
			else {
				var message = e.IsChecked == true ? "WifiAp Enabling is fail." : "WifiAp Disabling is fail.";
	//			_TetheringSwitch.Checked = e.IsChecked == true ? false : true;

				Toast.MakeText(_Application, message, ToastLength.Short).Show();
			}

#endif

		}

		/// <summary>
		/// Toggle Wifi access point switch
		/// </summary>
		/// <remarks>
		/// テザリング有効化前に自前でWifiをOffにする必要があるので、
		/// まずWifiをOffにする処理が入ってます.
		/// </remarks>
		/// <param name="enabled"></param>
		/// <returns></returns>
		async Task<bool> ToggleWifiApAsync(bool enabled)
		{
			bool result = false;

			int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
			Android.Util.Log.Info("Info", "WifiController.ToggleWifiApAsync({0}) ThreadID = {1}", enabled, threadID);


			// Enablingが完了したかチェック
			// todo: タイムアウト処理作る
			Func<Task<int>> EnablingCheck = async () =>
			{
				while(true) {
					int value = WifiUtility.GetWifiApState(_Application);
//					if(value != WifiAPState.Enabling) {
					if(value == WifiUtility.WifiAPState.Enabled) {
						return value;
					}
					await Task.Delay(100);	// 100ミリ待機
				}
			};

			// Disablingが完了したかチェック
			// todo: タイムアウト処理作る
			Func<Task<int>> DisablingCheck = async () =>
			{
				while(true) {
					int value = WifiUtility.GetWifiApState(_Application);
					if(value == WifiUtility.WifiAPState.Disabled) {
						return value;
					}
					await Task.Delay(100);	// 100ミリ待機
				}
			};

			// Wifi 有効(無効)化待ち
			// todo: タイムアウト処理作る
			Action<bool> WaitForWifiEnabed = (flag) =>
			{
				while (true){
					if (_WifiManager.IsWifiEnabled == flag){
						return;
					}
				}
			};


			int ApState = WifiUtility.GetWifiApState(_Application);

			if (enabled) {				// 有効化処理
				switch(ApState) {	// 現在のテザリングの状態によって処理を分ける
				case WifiUtility.WifiAPState.Enabled:
					result = true;
					break;
				case WifiUtility.WifiAPState.Disabled:
					// Switch Wifi Disabled
					WifiUtility.ToggleWifi(_Application, !enabled);

					// Waiting for Wifi Disabled.
					await Task.Run(() => WaitForWifiEnabed(!enabled));

					// Set WifiAp
					if(WifiUtility.SetWifiApEnabled(_Application, enabled) == false) {
						result = false;
						break;
					}

					// テザリングの有効化チェック
					int state = await EnablingCheck();
					if(state == WifiUtility.WifiAPState.Enabled) {
						result = true;
					}

					break;
				case WifiUtility.WifiAPState.Failed:
					break;
				default:
					Android.Util.Log.Error("Error", "Unprocessed WifiAPState Switch. WifiAPState = " + ApState.ToString());
					break;
				}

			}
			else {				// 無効化処理
				switch(ApState) {
				case WifiUtility.WifiAPState.Enabled:
					// Set WifiAp
					if(WifiUtility.SetWifiApEnabled(_Application, enabled) == false) {
						result = false;
						break;
					}

					// テザリングの無効化チェック
					int state = await DisablingCheck();
					if(state == WifiUtility.WifiAPState.Disabled) {
						result = true;
					}

					// Switch Wifi Enabled
//					await Task.Run(() => ToggleWifiAsync(!enabled));
//					ToggleWifi(!enabled);	// Wifi の有効化リクエストだけしてあとはほっといてOK


					break;
				case WifiUtility.WifiAPState.Disabled:
					result = true;
					break;
				case WifiUtility.WifiAPState.Failed:
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


		#endregion // TetheringMethod

		#endregion // Method

	}
}