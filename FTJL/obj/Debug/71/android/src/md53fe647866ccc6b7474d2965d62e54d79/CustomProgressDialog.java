package md53fe647866ccc6b7474d2965d62e54d79;


public class CustomProgressDialog
	extends android.app.Dialog
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onWindowFocusChanged:(Z)V:GetOnWindowFocusChanged_ZHandler\n" +
			"";
		mono.android.Runtime.register ("FTJL.Extensions.CustomProgressDialog, FTJL", CustomProgressDialog.class, __md_methods);
	}


	public CustomProgressDialog (android.content.Context p0)
	{
		super (p0);
		if (getClass () == CustomProgressDialog.class)
			mono.android.TypeManager.Activate ("FTJL.Extensions.CustomProgressDialog, FTJL", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
	}


	public CustomProgressDialog (android.content.Context p0, boolean p1, android.content.DialogInterface.OnCancelListener p2)
	{
		super (p0, p1, p2);
		if (getClass () == CustomProgressDialog.class)
			mono.android.TypeManager.Activate ("FTJL.Extensions.CustomProgressDialog, FTJL", "Android.Content.Context, Mono.Android:System.Boolean, mscorlib:Android.Content.IDialogInterfaceOnCancelListener, Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
	}


	public CustomProgressDialog (android.content.Context p0, int p1)
	{
		super (p0, p1);
		if (getClass () == CustomProgressDialog.class)
			mono.android.TypeManager.Activate ("FTJL.Extensions.CustomProgressDialog, FTJL", "Android.Content.Context, Mono.Android:System.Int32, mscorlib", this, new java.lang.Object[] { p0, p1 });
	}


	public void onWindowFocusChanged (boolean p0)
	{
		n_onWindowFocusChanged (p0);
	}

	private native void n_onWindowFocusChanged (boolean p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
