using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUFX
{

    /// <summary>
    /// Class holding the code for runtime layout creation, &#10; &#13; and for holding the logic used by the UI.<para/>
    /// This UI is intended to be spawned as a sub-window of the <see cref="ConfigurationGUI"/>, spawned on the click of the 'Edit Spline' button.
    /// </summary>
    /// <remarks>
    /// After creation it should be initialized by providing a callback for when the user has selected a value / pressed the update button.<para/>
    /// If another call is made to open the SplineConfigurationGUI while it is already displayed, the existing instance should be updated for the new parameter rather than creating a second window.<para/>
    /// The parent window should still respond to other interactions.  If another request is made for the spline window from a different parameter,
    /// any existing changes should be dropped, and the subwindow should be updated to reflect the newly selected spline (single-instance mode).
    /// Unknown how to best accomplish this UI; dynamic widget placement is not something that the IMGUI system can do well (i.e. dragging nodes to set curve values),
    /// and the text-based system used by the SSTU-SRB curve setup is... strange to use and configure with any accuracy.
    /// Possible to leverage the curve-editor used by the KAL?  Is this code in the stock DLL, or added through a sub-module?  Is it generic enough to re-use or buried in the stock code?
    /// </remarks>
    public class SplineConfigurationGUI
    {

    }

}
