using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace NMica.HttpModules.Logging
{
    /// <summary>
    /// Reflectively patch MVC error handling mechanism so we're not tied to specific MVC version
    /// </summary>
    [HarmonyPatch]
    public class HandleErrorAttributeOnErrorPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            var handleErrorAttributeType = Type.GetType("System.Web.Mvc.HandleErrorAttribute, System.Web.Mvc");
            if (handleErrorAttributeType != null)
            {            
                yield return AccessTools.Method(handleErrorAttributeType, "OnException");
            }
        }

        static void Postfix(object __instance, object __0)
        {
            try
            {
                if (__0 == null || __0.GetType().Name != "ExceptionContext")
                    return;
                dynamic filterContext = __0;
                if (filterContext.IsChildAction || !filterContext.HttpContext.IsCustomErrorEnabled)
                    return;
                Exception exception = filterContext.Exception;
                Console.Error.WriteLine(exception);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}