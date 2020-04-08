package md5db4d053b4dda896b1457a1ab896139a0;


public class LimeStoneActivity
	extends android.app.Activity
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreate:(Landroid/os/Bundle;)V:GetOnCreate_Landroid_os_Bundle_Handler\n" +
			"";
		mono.android.Runtime.register ("FTJL.LimeStoneActivity, FTJL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", LimeStoneActivity.class, __md_methods);
	}


	public LimeStoneActivity ()
	{
		super ();
		if (getClass () == LimeStoneActivity.class)
			mono.android.TypeManager.Activate ("FTJL.LimeStoneActivity, FTJL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}


	public void onCreate (android.os.Bundle p0)
	{
		n_onCreate (p0);
	}

	private native void n_onCreate (android.os.Bundle p0);

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
