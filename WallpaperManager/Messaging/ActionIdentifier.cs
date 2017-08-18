/*****************************************************************
 * Author: Norbert Eder
 * E-Mail: csharp@gmx.at
 * Blog  : http://devtyr.norberteder.com
 * 
 * Copyright © 2010 by Norbert Eder
 * 
 * ***************************************************************/

namespace DevTyr.Mvvm.Messaging
{
    public class ActionIdentifier
    {
        public WeakReferenceAction Action { get; set; }
        public string IdentificationCode { get; set; }
    }
}
