using Android.App;
using Android.Widget;
using Android.OS;

using Android.Net.Wifi;
using Android.Net;
using Android.Content;
using Android.Telephony;

using System.Reflection;
using System;


namespace NetworkDeviceSwitch
{
	[Activity(Label = "NetworkDeviceSwitch",MainLauncher = true,Icon = "@drawable/icon")]
	public class MainActivity:Activity
	{

		// NetworkStateReceiverのマニフェスト登録用文字列

		// ネットワーク切り替え時にブロードキャストされる
		public const string CONNECTIVITY_CHANGE = ConnectivityManager.ConnectivityAction;

		public const string PhoneStateChanged = TelephonyManager.ActionPhoneStateChanged;


		Switch mWifiSwitch = null;			// Wifiスイッチ
		Switch mMobileSwitch = null;		// モバイルデータスイッチ
		Switch mTetheringSwitch = null;		// テザリングスイッチ

		public TextView StatusView = null;	// ネットワークステータス表示用


		ConnectivityManager	mConnectivityManager = null;
		WifiManager			mWifiManager = null;
		TelephonyManager	mTelephonyManager = null;
		SubscriptionManager mSubscriptionManager = null;	// API22以降



		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			StatusView = FindViewById<TextView>(Resource.Id.StatusView);
			StatusView.Text = "";

			mWifiSwitch = FindViewById<Switch>(Resource.Id.WifiSwitch);
			mWifiSwitch.CheckedChange += OnWifiSwitchCheckedChange;

			mMobileSwitch = FindViewById<Switch>(Resource.Id.MobileSwitch);
			mMobileSwitch.CheckedChange += OnMobileDataSwitchCheckedChange;


			mTetheringSwitch = FindViewById<Switch>(Resource.Id.TetheringSwitch);


			mConnectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
			mWifiManager = (WifiManager)GetSystemService(WifiService);
			mTelephonyManager = (TelephonyManager)GetSystemService(TelephonyService);
			if(Build.VERSION.SdkInt > BuildVersionCodes.Lollipop) {
				mSubscriptionManager = (SubscriptionManager)GetSystemService(TelephonySubscriptionService);
			}

	//		mConnectivityManager.StopUsingNetworkFeature(ConnectivityType.Mobile, "");

			// このActivityの間だけブロードキャストされればいいので以下の方法で登録する
			// 通信状況取得用レシーバー登録
			var NetintentFilter = new IntentFilter();
			NetintentFilter.AddAction(CONNECTIVITY_CHANGE);
			NetworkStateReceiver NetStateReceiver = new NetworkStateReceiver(this, mConnectivityManager, mWifiManager);
			RegisterReceiver(NetStateReceiver, NetintentFilter);


			InitializeView();

		}


		/// <summary>
		/// 各種ビューの初期化
		/// </summary>
		void InitializeView()
		{

			if(mWifiManager.IsWifiEnabled) {
				mWifiSwitch.Checked = true;
			}

		}

		/// <summary>
		/// Called when the Wifi switch changes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnWifiSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if(e.IsChecked) {
				if(ToggleWifi(e.IsChecked)) {
					Toast.MakeText(this, "Wifi Enabled.",ToastLength.Short).Show();
				}
			}
			else {
				if(ToggleWifi(e.IsChecked)) {
					Toast.MakeText(this, "Wifi Disabled.",ToastLength.Short).Show();
				}	
			}
		}

		/// <summary>
		/// Called when the Mobiledata switch changes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnMobileDataSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if(e.IsChecked) {
				if(ToggleMobileData(e.IsChecked)) {
					Toast.MakeText(this, "MobileData Enabled.", ToastLength.Short).Show();
				}
			}
			else {
				if(ToggleMobileData(e.IsChecked)) {
					Toast.MakeText(this, "MobileData Disabled.", ToastLength.Short).Show();
				}
			}
		}

		/// <summary>
		/// Called when the Tethering switch changes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnTetheringSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			// ConnectivityManager, WifiManagerあたりで何か探してみる
			// Wifi-Access Pointとかで調べる
			// https://github.com/xamarin/xamarin-android githubのこれ見てみる
		}

		/// <summary>
		/// Toggle Wifi switch
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		bool ToggleWifi(bool enabled)
		{
			bool result = false;
			if(mWifiManager.IsWifiEnabled != enabled) {
				mWifiManager.SetWifiEnabled(enabled);
				result = true;
			}

			return result;
		}

		/// <summary>
		/// Toggle mobile data switch
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		bool ToggleMobileData(bool enabled)
		{
			bool result = false;
			try {
				// Lollipop以降の実装
				if(Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
			//		settings
					// クラスインスタンス取得
//					Java.Lang.Class telephonManClass = Java.Lang.Class.ForName(mTelephonyManager.Class.Name);
//					Java.Lang.Reflect.Field[] fields = telephonManClass.GetDeclaredFields();
//					if(enabled == true) {
//						Console.WriteLine("##### Start Field List #####");
//						foreach(var field in fields) {
//							Console.WriteLine(field.Name);
//						}
//						Console.WriteLine("##### End Field List #####");
//						Java.Lang.Reflect.Method[] methods = telephonManClass.GetDeclaredMethods();
//						Console.WriteLine("##### Start Methd List #####");
//						foreach(var method in methods) {
//							Console.WriteLine(method.Name);
//						}
//						Console.WriteLine("##### End Methd List #####");
//					}
					ToggleMobileDatafromL(enabled);
				}	// Gingerbread以上 KitkatWatch以下の実装
				if(Build.VERSION.SdkInt <= BuildVersionCodes.KitkatWatch
						&& Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread) {

					ToggleMobileDatafromGtoK(enabled);

				}	// Gingerbread未満の実装
				else if(Build.VERSION.SdkInt < BuildVersionCodes.Gingerbread) {
					// no op
				}

			}
			catch(Exception e) {
				System.Console.WriteLine(e);
			}

			return result;
		}

		/// <summary>
		/// Toggle mobiledata enabled.
		/// From Lollipop.
		/// Reference http://stackoverflow.com/questions/26539445/the-setmobiledataenabled-method-is-no-longer-callable-as-of-android-l-and-later
		/// </summary>
		/// <param name="enabled"></param>
		/// <returns></returns>
		bool ToggleMobileDatafromL(bool enabled)
		{
			bool result = false;

			int state = 0;
			try {

				state = IsMobileDataEnabledFromLollipos() ? 0 : 1;

				if(state == 1) {
					Console.WriteLine("Mobile Data Enabled. from lollipop");
				}
				else {
					Console.WriteLine("Mobile Data Disabled. from lollipos");
				}

				// Lollipopよりバージョンが上(API 22以上)かどうかで更に挙動が変わる
				if(Build.VERSION.SdkInt > BuildVersionCodes.Lollipop) {
				
				}
				else if(Build.VERSION.SdkInt == BuildVersionCodes.Lollipop) { // Lollipop(API 21)

				}

			}
			catch(Exception e) {
				
			}

			return result;
		}

		/// <summary>
		/// Mobile data enabled check
		/// </summary>
		/// <param name="context"></param>
		/// <returns>mobile data is disable. return true</returns>
		bool IsMobileDataEnabledFromLollipos()
		{
			bool state = false;
			if(Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) {
				state = Android.Provider.Settings.Global.GetInt(ContentResolver, "mobile_data", 0) == 1;
			}

			return state;
		}

		/// <summary>
		/// Toggle MobileData Enabled.
		/// From Gingerbread to KitkatWatch.
		/// </summary>
		/// <param name="enabled"></param>
		bool ToggleMobileDatafromGtoK(bool enabled)
		{
			bool result = false;

			try {
				// Javaのリフレクションを使用してsetMobileDataEnabledメソッドとgetMobileDataEnabledメソッドにアクセス

				Java.Lang.Class connManClass = Java.Lang.Class.ForName(mConnectivityManager.Class.Name);
				Java.Lang.Reflect.Field iConnectivityManagerField = connManClass.GetDeclaredField("mService");
				iConnectivityManagerField.Accessible = true;
				Java.Lang.Object iConnectivityManager = iConnectivityManagerField.Get(mConnectivityManager);
				Java.Lang.Class iConnectivityManagerClass = Java.Lang.Class.ForName(iConnectivityManager.Class.Name);
				Java.Lang.Reflect.Method setMobileDataEnableMethod = iConnectivityManagerClass.GetDeclaredMethod("setMobileDataEnabled", Java.Lang.Boolean.Type);
				setMobileDataEnableMethod.Accessible = true;
				Java.Lang.Reflect.Method getMobileDataEnableMethod = iConnectivityManagerClass.GetDeclaredMethod("getMobileDataEnabled");
				getMobileDataEnableMethod.Accessible = true;

				// 現在の状態と違ってたら設定する
				// Lollipop以降は Java.Lang.NoSuchMethodException の例外が発生することを確認
				if(enabled != (bool)getMobileDataEnableMethod.Invoke(iConnectivityManager)) {
					setMobileDataEnableMethod.Invoke(iConnectivityManager, enabled);
					result = true;
				}
			}
			catch(Exception e) {
				throw e;
			}

			return result;
		}


	}
}

