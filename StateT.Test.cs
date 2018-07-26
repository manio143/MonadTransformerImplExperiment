using System;
using Great.Monads;

namespace Great.Monads
{
    public class Identity<A>
    {
        public A Value { get; }

        public Identity(A value)
        {
            Value = value;
        }

        public static Identity<B> Return<B>(B x)
        {
            // Console.WriteLine("Identity.Return");
            return new Identity<B>(x);
        }

        public static Identity<B> Bind<B>(Identity<A> m, Func<A, Identity<B>> f)
        {
            // Console.WriteLine("Identity.Bind");
            return f.Invoke(m.Value);
        }

        public static Identity<A> Fail<B>(B x)
        {
            throw new Exception("An unexpected error occured:\n" + x.ToString());
        }
    }
}

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            var x = StateT<float, Identity<object>, float>.Return(1.0f);
            var y = StateT<int, Identity<object>, string>.Return("abc");
            var z = StateT<int, Identity<object>, string>.Bind(y, Length);
            // var zz = StateT<int, Identity<object>, int>.Run(z, 8);
            // Console.WriteLine(zz.Value);
            for(int i = 0; i < 100000; i++)
                StateT<int, Identity<object>, int>.Run(z, 0);
            var zz = StateT<int, Identity<object>, int>.Run(z, 0);
            var xx = StateT<float, Identity<object>, float>.Run(x, 0.1f);
            Console.WriteLine("x: {0}", xx.Value);
            Console.WriteLine("z: {0}", zz.Value);
        }

        public static StateT<int, Identity<object>, int> Length(string s)
        {
            return StateT<int, Identity<object>, int>.Return(s.Length);
        }
    }
}