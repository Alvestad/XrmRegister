using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public static class XrmExtensions
    {
        public static Entity GetPreImage(this IPluginExecutionContext pluginExecutionContext, string name)
        {
            if (pluginExecutionContext == null)
                return null;
            if (pluginExecutionContext.PreEntityImages == null)
                return null;
            if (pluginExecutionContext.PreEntityImages.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (pluginExecutionContext.PreEntityImages.ContainsKey(name))
                    return pluginExecutionContext.PreEntityImages[name];
                else
                    return null;
            }
            else
            {
                return pluginExecutionContext.PreEntityImages.First().Value;
            }
        }

        public static Entity GetPostImage(this IPluginExecutionContext pluginExecutionContext, string name)
        {
            if (pluginExecutionContext == null)
                return null;
            if (pluginExecutionContext.PostEntityImages == null)
                return null;
            if (pluginExecutionContext.PostEntityImages.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (pluginExecutionContext.PostEntityImages.ContainsKey(name))
                    return pluginExecutionContext.PostEntityImages[name];
                else
                    return null;
            }
            else
            {
                return pluginExecutionContext.PostEntityImages.First().Value;
            }
        }

        public static T GetTarget<T>(this IPluginExecutionContext pluginExecutionContext) where T : Entity
        {
            if (pluginExecutionContext == null)
                return null;
            if (pluginExecutionContext.InputParameters == null)
                return null;
            if (!pluginExecutionContext.InputParameters.Contains("Target"))
                return null;

            return pluginExecutionContext.InputParameters["Target"] as T;
        }

        public static OrganizationServiceContext GetCrmContext(this IOrganizationService service)
        {
            if (service == null)
                return null;
            return new OrganizationServiceContext(service);
        }

        //    public static string NameOf(this object value)
        //    {
        //        return nameof(value).ToLower();
        //    }

        //    public static string AttributeLogicalName<T, Prop>(this T entity, Expression<Func<T, Prop>> selector) where T : Entity
        //    {
        //        var member = selector.Body as MemberExpression;
        //        if (member == null) throw new Exception("The selector must be a member expression");

        //        var prop = member.Member;
        //        var logicalNameAttr = prop.GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true).Cast<AttributeLogicalNameAttribute>().FirstOrDefault();

        //        if (logicalNameAttr == null)
        //        {    
        //            return null;
        //        }

        //        return logicalNameAttr.LogicalName;
        //    }
        //    public static string[] AttributeLogicalNameList<T>(this T entity, params Expression<Func<T, object>>[] selectors) where T : Entity
        //    { 
        //        var value = new List<string>();

        //        foreach (var selector in selectors)
        //        {

        //            var member = selector.Body as MemberExpression;
        //            if (member == null) throw new Exception("The selector must be a member expression");

        //            var prop = member.Member;
        //            var logicalNameAttr = prop.GetCustomAttributes(typeof(AttributeLogicalNameAttribute), true).Cast<AttributeLogicalNameAttribute>().FirstOrDefault();

        //            if (logicalNameAttr == null)
        //            {        
        //                return null;
        //            }

        //            value.Add(logicalNameAttr.LogicalName);
        //        }

        //        return value.ToArray();
        //    }
    }
}
