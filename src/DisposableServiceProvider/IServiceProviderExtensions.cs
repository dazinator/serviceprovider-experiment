using System;

namespace ScopeTrackingServiceProvider
{
    public static class IServiceProviderExtensions
    {      
        /// <summary>
        /// Decorates the specified IServiceProvider with a ScopeTrackingServiceProvider and returns it, 
        /// only if the IServiceProvider to be decorated implements <see cref="IDisposable"/>. Otherwise returns 
        /// the original undecorated IServiceProvider as is. 
        /// This is because scope tracking behaviour is intended to enable safe disposal so if
        /// the service provider can't be disposed then there is no point to scope tracking.
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        public static IServiceProvider DecorateIfDisposable(this IServiceProvider sp)
        {
            if (sp is IDisposable)
            {
                var spType = sp.GetType();
                var newSpType = typeof(ScopeTrackingServiceProvider<>).MakeGenericType(spType);
                return (IServiceProvider)Activator.CreateInstance(newSpType, new object[] { sp });
            }

            return sp;            
        }
    }


}
