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
    interface IActionParameter
    {
        void ExecuteWithParameter(object parameter);
    }
}
