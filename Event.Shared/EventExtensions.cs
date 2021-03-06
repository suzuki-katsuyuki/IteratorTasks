﻿#if UseIteratorTasks
using IteratorTasks;
#else
using System.Threading;
using System.Threading.Tasks;
#endif

using System.Reactive.Disposables;

namespace System
{
    /// <summary>
    /// <see cref="IEvent{T}"/>に対する拡張メソッド。
    /// </summary>
    public static partial class EventExtensions
    {
        #region Task 化、CancellationToken 化

        /// <summary>
        /// イベントが起きた時にキャンセルするキャンセルトークンに変換。
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static CancellationToken ToCancellationToken<TArg>(this IEvent<TArg> e)
        {
            var cts = new CancellationTokenSource();
            e.Subscribe(cts.Cancel);
            return cts.Token;
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, CancellationToken ct)
        {
            return FirstAsync(e, _ => true, ct);
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e)
        {
            return FirstAsync(e, CancellationToken.None);
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// predicate を満たすまでは何回でもイベントを受け取る。
        /// </summary>
        /// <typeparam name="TArg">イベント引数の型。</typeparam>
        /// <param name="e">イベント発生元。</param>
        /// <param name="predicate">受け取り条件。</param>
        /// <param name="ct">キャンセル用。</param>
        /// <returns>イベントが1回起きるまで待つタスク。</returns>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, Func<TArg, bool> predicate, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TArg>();

            IDisposable subscription = null;

            subscription = e.Subscribe((sender, args) =>
            {
                if (predicate(args))
                {
                    subscription.Dispose();
                    tcs.TrySetResult(args);
                }
            });

            if (ct != CancellationToken.None)
            {
                ct.Register(() =>
                {
                    subscription.Dispose();
                    tcs.TrySetCanceled();
                });
            }

            return tcs.Task;
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// predicate を満たすまでは何回でもイベントを受け取る。
        /// predicate が非同期処理なバージョン。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, Func<TArg, Task<bool>> predicate, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TArg>();

            IDisposable subscription = null;

            subscription = e.Subscribe((sender, args) =>
            {
                predicate(args).ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        subscription.Dispose();
                        tcs.TrySetResult(args);
                    }
                });
            });

            if (ct != CancellationToken.None)
            {
                ct.Register(() =>
                {
                    subscription.Dispose();
                    tcs.TrySetCanceled();
                });
            }

            return tcs.Task;
        }

        #endregion
        #region Subscribe

        /// <summary>
        /// イベントを購読する。
        /// </summary>

        public static IDisposable Subscribe<T>(this IEvent<T> e, Action<T> handler)
        {
            return e.Subscribe((_1, arg) => handler(arg));
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IEvent<T> e, Action handler)
        {
            return e.Subscribe((_1, _2) => handler());
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Handler<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Action<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Action handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        #endregion
        #region Subscribe 非同期版

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, AsyncHandler<T> handler)
        {
            e.Add(handler);
            return Disposable.Create(() => e.Remove(handler));
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, Func<T, Task> handler)
        {
            return Subscribe(e, (_1, arg) => handler(arg));
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, Func<Task> handler)
        {
            return Subscribe(e, (_1, _2) => handler());
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, AsyncHandler<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, Func<T, Task> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        /// <summary>
        /// キャンセルされるまでの間イベントを購読する。
        /// </summary>
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, Func<Task> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        #endregion
        #region object から具体的な型へのキャスト

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <remarks>
        /// <paramref name="e"/> の Subscribe に対して Dispose するすべがないんで戻り値で返したイベントの寿命に注意。
        /// </remarks>
        [Obsolete("Use the System.Events.Event.Cast instead")]
        public static IEvent<T> Cast<T>(this IEvent<object> e)
        {
            var h = new HandlerList<T>();
            e.Subscribe((sender, arg) => h.Invoke(sender, (T)arg));
            return h;
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IEvent<object> e, Handler<T> handler)
        {
            Handler<object> objHandler = (sender, arg) =>
            {
                if(arg is T)
                    handler(sender, (T)arg);
            };
            return e.Subscribe(objHandler);
        }

        /// <summary>
        /// イベントを購読する。
        /// </summary>
        public static IDisposable Subscribe<T>(this IEvent<object> e, Action<T> handler)
        {
            return e.Subscribe((_1, arg) =>
            {
                if (arg is T)
                    handler((T)arg);
            });
        }

        #endregion
    }
}
