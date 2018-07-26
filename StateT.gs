module Great.Monads

    union StateT<S, M, A> where M : Monad<M> = StateT of (S -> M<(A,S)>)
        with
            public static Run(StateT(f), s) => f(s)
            public static Return(x) => StateT <| (s) => do return (x,s)
            public static Bind(m, f) =>
                StateT <| (s) => do
                    let (r, s') <- Run(m, s)
                    Run(f(r), s')
            public static Fail(x) => StateT <| (s) => do fail x
            public static Lift(m) => StateT <| (s) => do
                                        let x <- m
                                        return (x,s)
    // StateT<#S, #M> is a Monad
    // StateT<#S> is a MonadTransformer
    