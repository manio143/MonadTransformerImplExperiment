using System;
using System.Linq;
using System.Reflection;

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


        private static MethodInfo returnM;
        private static MethodInfo bindM;
        private static MethodInfo failM;

        private static void GetInnerMonadMethods()
        {
            if (bindM == null)
                bindM = typeof(M).GetMethod("Bind", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(new[]{typeof(object)});
            if (returnM == null)
                returnM = typeof(M).GetMethod("Return", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(new[]{typeof(object)});
            if (failM == null)
                failM = typeof(M).GetMethod("Fail", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(new[]{typeof(object)});
        }
        public static StateT<S, M, B> Return<B>(B x)
        {
            // Console.WriteLine("StateT.Return");
            GetInnerMonadMethods();
            return new StateT<S,M,B>((s) => 
                (M) returnM.Invoke(null, new object[]{new Tuple<B, S>(x, s)}));
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
                return (M) bindM.Invoke(null, new object[]{tup, binder});
                });
        }

        public static StateT<S, M, A> Fail<B>(B x)
        {
            GetInnerMonadMethods();
            return new StateT<S, M, A>((s) => 
                (M) failM.Invoke(null, new object[]{x}));
        }

        public static StateT<S, M, A> Lift(M monad)
        {
            GetInnerMonadMethods();
            Func<S, Func<A, M>> binder = (s) => (x) => (M) returnM.Invoke(null, new object[]{new Tuple<A, S>(x, s)});
            return new StateT<S, M, A>((s) => 
                (M) bindM.Invoke(null, new object[]{monad, binder(s)}));
        }
    }

}