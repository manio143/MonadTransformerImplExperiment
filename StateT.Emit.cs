using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Great.Monads
{
    public class StateT<S, M, A>
    {
        // NOTE: single constructor unions do not expand to class hierarchies
        public Func<S, M> Value { get; }
        
        public StateT(Func<S, M> value) {
            Value = value;
        }

        public static M Run(StateT<S, M, A> monad, S state)
        {
            return monad.Value.Invoke(state);
        }


        private static Func<object, M> returnM;
        private static Func<M, Func<object, M>, M> bindM;
        private static Func<object, M> failM;

        private static TDelegate EmitDelegate<TDelegate, TTarget>(string name) where TDelegate : class
        {
            var methodInfo = typeof(M)
                .GetMethod(name, BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(new[]{typeof(object)});
            var delegateTypes = typeof(TDelegate).GetGenericArguments();
            var delegateReturn = delegateTypes[delegateTypes.Length - 1];
            var delegateParams = new Type[delegateTypes.Length - 1];
            Array.Copy(delegateTypes, delegateParams, delegateParams.Length);
            var dyn = new DynamicMethod(name + "M", delegateReturn, delegateParams, typeof(StateT<S,M,A>).Module);
            var il = dyn.GetILGenerator();
            if (delegateParams.Length >= 1)
                il.Emit(OpCodes.Ldarg_0);
            if (delegateParams.Length >= 2)
                il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, methodInfo);
            il.Emit(OpCodes.Ret);
            return dyn.CreateDelegate(typeof(TDelegate)) as TDelegate;
        }

        private static void GetInnerMonadMethods()
        {
            if (bindM == null)
                bindM = EmitDelegate<Func<M, Func<object, M>, M>, M>("Bind");
            if (returnM == null)
                returnM = EmitDelegate<Func<object, M>, M>("Return");
            if (failM == null)
                failM = EmitDelegate<Func<object, M>, M>("Fail");
        }
        
        public static StateT<S, M, B> Return<B>(B x)
        {
            // Console.WriteLine("StateT.Return");
            GetInnerMonadMethods();
            return new StateT<S,M,B>((s) => 
                returnM(new Tuple<B, S>(x, s)));
        }

        public static StateT<S, M, B> Bind<B>(StateT<S, M, A> monad, Func<A,StateT<S, M, B>> f)
        {
            GetInnerMonadMethods();
            // Console.WriteLine("StateT.Bind");
            Func<object, M> binder = (o) => {
                // Console.WriteLine("StateT.Bind.binder");
                // Console.WriteLine(o.GetType().Name);
                // Console.WriteLine(typeof(Tuple<A,S>).Name);
                var t = (Tuple<A,S>) o;
                return StateT<S, M, B>.Run(f(t.Item1), t.Item2);
            };
            // Console.WriteLine("S = {0}", typeof(S).Name);
            // var typeM = typeof(M);
            // Console.WriteLine("M = {0}", typeM.Name);
            // Console.WriteLine("A = {0}", typeof(A).Name);
            // Console.WriteLine(String.Join(", ", typeM.GetMethods(BindingFlags.Static | BindingFlags.Public).Select(mi => mi.Name)));

            // Console.WriteLine(bindM?.ToString() ?? "Unable to find Bind on M");
            return new StateT<S, M, B>((s) => {
                // Console.WriteLine("StateT.Bind.invoker");
                var tup = StateT<S, M, A>.Run(monad, s);
                // Console.WriteLine(tup.GetType());
                // Console.WriteLine(binder.GetType());
                return bindM(tup, binder);
                });
        }

        public static StateT<S, M, A> Fail<B>(B x)
        {
            GetInnerMonadMethods();
            return new StateT<S, M, A>((s) => 
                failM(x));
        }

        public static StateT<S, M, A> Lift(M monad)
        {
            GetInnerMonadMethods();
            Func<S, Func<object, M>> binder = (s) => (x) => returnM(new Tuple<object, S>(x, s));
            return new StateT<S, M, A>((s) => 
                bindM(monad, binder(s)));
        }
    }

}