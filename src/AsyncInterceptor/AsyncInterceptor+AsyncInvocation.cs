// Copyright (c) 2020 stakx
// License available at https://github.com/stakx/AsyncInterceptor/blob/master/LICENSE.md.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Castle.DynamicProxy.Contrib
{
    partial class AsyncInterceptor
    {
        private sealed class AsyncInvocation : IAsyncInvocation
        {
            private readonly IInvocation invocation;
            private readonly IInvocationProceedInfo proceed;

            public AsyncInvocation(IInvocation invocation)
            {
                this.invocation = invocation;
                this.proceed = invocation.CaptureProceedInfo();
            }

            public IReadOnlyList<object> Arguments => invocation.Arguments;

            public MethodInfo Method => this.invocation.Method;

            public object Result { get; set; }

            public ValueTask ProceedAsync()
            {
                var previousReturnValue = this.invocation.ReturnValue;
                try
                {
                    proceed.Invoke();
                    var returnValue = invocation.ReturnValue;
                    if (returnValue != previousReturnValue)
                    {
                        var awaiter = returnValue.GetAwaiter();
                        if (awaiter.IsCompleted())
                        {
                            this.Result = awaiter.GetResult();
                            return default;
                        }
                        else
                        {
                            var tcs = new TaskCompletionSource<bool>();
                            awaiter.OnCompleted(() =>
                            {
                                try
                                {
                                    this.Result = awaiter.GetResult();
                                    tcs.SetResult(true);
                                }
                                catch (Exception exception)
                                {
                                    tcs.SetException(exception);
                                }
                            });
                            return new ValueTask(tcs.Task);
                        }
                    }
                    else
                    {
                        return default;
                    }
                }
                finally
                {
                    invocation.ReturnValue = previousReturnValue;
                }
            }
        }
    }
}