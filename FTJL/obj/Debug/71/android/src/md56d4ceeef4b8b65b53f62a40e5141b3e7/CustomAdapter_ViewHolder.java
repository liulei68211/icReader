package md56d4ceeef4b8b65b53f62a40e5141b3e7;


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
		mono.android.Runtime.register ("FTJL.Adapter.CustomAdapter+ViewHolder, FTJL", CustomAdapter_ViewHolder.class, __md_methods);
	}


	public CustomAdapter_ViewHolder ()
	{
		super ();
		if (getClass () == CustomAdapter_ViewHolder.class)
			mono.android.TypeManager.Activate ("FTJL.Adapter.CustomAdapter+ViewHolder, FTJL", "", this, new java.lang.Object[] {  });
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
