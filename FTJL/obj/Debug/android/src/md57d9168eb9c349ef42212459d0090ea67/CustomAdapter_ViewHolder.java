package md57d9168eb9c349ef42212459d0090ea67;


public class CustomAdapter_ViewHolder
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("FTJL.Adapter.CustomAdapter+ViewHolder, FTJL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", CustomAdapter_ViewHolder.class, __md_methods);
	}


	public CustomAdapter_ViewHolder ()
	{
		super ();
		if (getClass () == CustomAdapter_ViewHolder.class)
			mono.android.TypeManager.Activate ("FTJL.Adapter.CustomAdapter+ViewHolder, FTJL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

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
