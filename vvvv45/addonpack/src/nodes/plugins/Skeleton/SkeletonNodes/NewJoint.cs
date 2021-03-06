#region licence/info

//////project name
//vvvv plugin template

//////description
//basic vvvv node plugin template.
//Copy this an rename it, to write your own plugin node.

//////licence
//GNU Lesser General Public License (LGPL)
//english: http://www.gnu.org/licenses/lgpl.html
//german: http://www.gnu.de/lgpl-ger.html

//////language/ide
//C# sharpdevelop 

//////dependencies
//VVVV.PluginInterfaces.V1;
//VVVV.Utils.VColor;
//VVVV.Utils.VMath;

//////initial author
//vvvv group

#endregion licence/info

//use what you need
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Text.RegularExpressions;
using System.Globalization;
using VVVV.Utils.SharedMemory;
using VVVV.SkeletonInterfaces;

//the vvvv node namespace
namespace VVVV.Nodes
{
	
	//class definition
	public class NewJoint: IPlugin, IDisposable
    {	          	
    	#region field declaration
    	
    	//the host (mandatory)
    	private IPluginHost FHost; 
    	// Track whether Dispose has been called.
   		private bool FDisposed = false;

    	private ITransformIn FBaseTransformInput;
    	private IValueIn FRotationConstraintsInput;
    	private IStringIn FJointNameInput;
    	private IValueConfig FChildrenCountInput;

    	private INodeOut FSkeletonOutput;
    	
		private ISkeleton outputSkeleton;
		private IJoint rootJoint;
		private List<INodeIn> childPins;
    
    	private bool initialized = false;
    	
    	private System.Guid[] guids;
    	
    	#endregion field declaration
       
    	#region constructor/destructor
    	
        public NewJoint()
        {
			//the nodes constructor
			//nothing to declare for this node
			rootJoint = new JointInfo();
			rootJoint.Id = 0;
			outputSkeleton = new Skeleton(rootJoint);
			outputSkeleton.BuildJointTable();
			childPins = new List<INodeIn>();
			
			guids = new System.Guid[1];
	    	guids[0] = new Guid("AB312E34-8025-40F2-8241-1958793F3D39");
		}
        
        // Implementing IDisposable's Dispose method.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
        	Dispose(true);
        	// Take yourself off the Finalization queue
        	// to prevent finalization code for this object
        	// from executing a second time.
        	GC.SuppressFinalize(this);
        }
        
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
        	// Check to see if Dispose has already been called.
        	if(!FDisposed)
        	{
        		if(disposing)
        		{
        			// Dispose managed resources.
        		}
        		// Release unmanaged resources. If disposing is false,
        		// only the following code is executed.
	        	
        		FHost.Log(TLogType.Debug, "PluginTemplate is being deleted");
        		
        		// Note that this is not thread safe.
        		// Another thread could start disposing the object
        		// after the managed resources are disposed,
        		// but before the disposed flag is set to true.
        		// If thread safety is necessary, it must be
        		// implemented by the client.
        	}
        	FDisposed = true;
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~NewJoint()
        {
        	// Do not re-create Dispose clean-up code here.
        	// Calling Dispose(false) is optimal in terms of
        	// readability and maintainability.
        	Dispose(false);
        }
        #endregion constructor/destructor
        
        #region node name and infos
       
        //provide node infos 
        private static IPluginInfo FPluginInfo;
        public static IPluginInfo PluginInfo
	    {
	        get 
	        {
	        	if (FPluginInfo == null)
				{
					//fill out nodes info
					//see: http://www.vvvv.org/tiki-index.php?page=vvvv+naming+conventions
					FPluginInfo = new PluginInfo();
					
					//the nodes main name: use CamelCaps and no spaces
					FPluginInfo.Name = "Joint";
					//the nodes category: try to use an existing one
					FPluginInfo.Category = "Skeleton Join";
					//the nodes version: optional. leave blank if not
					//needed to distinguish two nodes of the same name and category
					FPluginInfo.Version = "";
					
					//the nodes author: your sign
					FPluginInfo.Author = "Matthias Zauner";
					//describe the nodes function
					FPluginInfo.Help = "NewJoint";
					//specify a comma separated list of tags that describe the node
					FPluginInfo.Tags = "";
					
					//give credits to thirdparty code used
					FPluginInfo.Credits = "";
					//any known problems?
					FPluginInfo.Bugs = "certainly";
					//any known usage of the node that may cause troubles?
					FPluginInfo.Warnings = "";
					
					//leave below as is
					System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
					System.Diagnostics.StackFrame sf = st.GetFrame(0);
					System.Reflection.MethodBase method = sf.GetMethod();
					FPluginInfo.Namespace = method.DeclaringType.Namespace;
					FPluginInfo.Class = method.DeclaringType.Name;
					//leave above as is
				}
				return FPluginInfo;
	        }
		}

        public bool AutoEvaluate
        {
        	//return true if this node needs to calculate every frame even if nobody asks for its output
        	get {return false;}
        }
        
        #endregion node name and infos
        
      	#region pin creation
        
        //this method is called by vvvv when the node is created
        public void SetPluginHost(IPluginHost Host)
	    {
        	//assign host
	    	FHost = Host;
	    	

	    	//create inputs
	    	
	    	FHost.CreateStringInput("Joint Name", TSliceMode.Single, TPinVisibility.True, out FJointNameInput);
	    	
	    	FHost.CreateTransformInput("Base Transform", TSliceMode.Single, TPinVisibility.True, out FBaseTransformInput);
	    	
	    	String[] dimensions = new String[2];
	    	dimensions[0] = "Min";
	    	dimensions[1] = "Max";
	    	FHost.CreateValueInput("Rotation Constraints", 2, dimensions, TSliceMode.Dynamic, TPinVisibility.True, out FRotationConstraintsInput);
	    	FRotationConstraintsInput.SetSubType2D(-1.0, 1.0, 0.1, -1.0, 1.0, false, false, false);
	    	
	    	
	    	INodeIn node;
	    	FHost.CreateNodeInput("Child1", TSliceMode.Single, TPinVisibility.True, out node);
	    	node.SetSubType(guids, "Skeleton");
	    	childPins.Add(node);
	    	
	    	FHost.CreateValueConfig("Children Count", 1, null, TSliceMode.Single, TPinVisibility.OnlyInspector, out FChildrenCountInput);
	    	FChildrenCountInput.SetSubType(0,50, 1.0, 1.0, false, false, true);
	    	
	    	
	    	
	    	// create outputs
	    	
	    	FHost.CreateNodeOutput("Skeleton", TSliceMode.Single, TPinVisibility.True, out FSkeletonOutput);
	    	FSkeletonOutput.SetSubType(guids, "Skeleton");
	    	FSkeletonOutput.MarkPinAsChanged();
	    
	    	
        }

        #endregion pin creation
        
        #region mainloop
        
        public void Configurate(IPluginConfig Input)
        {	
        	if (Input.Name=="Children Count")
        	{
        		IValueConfig valueInput = (IValueConfig)Input;
        		double pinCount;
        		valueInput.GetValue(0, out pinCount);
        		
        		int oldChildrenCount = childPins.Count;
        		for (int i=oldChildrenCount-1; i>=(int)pinCount; i--)
        		{
        			FHost.DeletePin(childPins[i]);
        		}
        		for (int i=oldChildrenCount-1; i>=(int)pinCount; i--)
        		{
        			childPins.RemoveAt(i);
        		}
        		
        		INodeIn node;
        		for (int i=oldChildrenCount; i<pinCount; i++)
        		{
        			FHost.CreateNodeInput("Child"+(i+1), TSliceMode.Single, TPinVisibility.True, out node);
	    			node.SetSubType(guids, "Skeleton");
					childPins.Add(node);
        		}
        	}
        }
        
        //here we go, thats the method called by vvvv each frame
        //all data handling should be in here
        public void Evaluate(int SpreadMax)
        {     	
        	//if any of the inputs has changed
        	//recompute the outputs
        	
        	
        	
        	if (FJointNameInput.PinIsChanged || !initialized)
        	{
        		string name;
        		FJointNameInput.GetString(0, out name);
        		rootJoint.Name = name;
        		outputSkeleton.BuildJointTable();
        		FSkeletonOutput.MarkPinAsChanged();
        	}
        	
        	bool childrenChanged = false;
        	for (int i=0; i<childPins.Count; i++)
        	{
        		if (childPins[i].PinIsChanged)
        			childrenChanged = true;
        	}
        	
        	if (childrenChanged)
        	{
	        	outputSkeleton.ClearAll();
	        	outputSkeleton.Root = rootJoint;
	        	outputSkeleton.BuildJointTable();
	        	
	        	for (int i=0; i<childPins.Count; i++)
	        	{
	        		if (true) //childPinsList[i].PinIsChanged)
	        		{
	        			FSkeletonOutput.MarkPinAsChanged();
	        			if (childPins[i].IsConnected)
	        			{
	        				object currInterface;
	        				childPins[i].GetUpstreamInterface(out currInterface);
	        				ISkeleton subSkeleton = (ISkeleton)currInterface;
	        				IJoint child = subSkeleton.Root.DeepCopy();
	        				outputSkeleton.InsertJoint(outputSkeleton.Root.Name, child);
		        			outputSkeleton.BuildJointTable();
	        			}
	        		}
	        	}
	        	
	        	// re-calculate the IDs ...
	        	int currId = 0;
				foreach (KeyValuePair<string, IJoint> pair in outputSkeleton.JointTable)
				{
					pair.Value.Id = currId;
					currId++;
				}
        	}
        	
        	if (FBaseTransformInput.PinIsChanged)
        	{
        		Matrix4x4 baseTransform;
        		FBaseTransformInput.GetMatrix(0, out baseTransform);
        		rootJoint.BaseTransform = baseTransform;
        		FSkeletonOutput.MarkPinAsChanged();
        	}
        	
        	if (FRotationConstraintsInput.PinIsChanged)
        	{
        		rootJoint.Constraints.Clear();
	        	for (int i=0; i<3; i++)
	        	{
	        		double from, to;
	        		FRotationConstraintsInput.GetValue2D(i, out from, out to);
	        		rootJoint.Constraints.Add(new Vector2D(from, to));
	        	}
	        	FSkeletonOutput.MarkPinAsChanged();
        	}
        
        	FSkeletonOutput.SetInterface(outputSkeleton);
        	
        	initialized = true;
        }
             
        #endregion mainloop  
        
        #region helper
        
        
        
        #endregion helper
        
        
        
	}
}
