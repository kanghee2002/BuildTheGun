using System.Collections.Generic;
using BuildTheGun.Combat.Effects.Handlers;

namespace BuildTheGun.Combat.Effects
{
    /// <summary>
    /// <see cref="EffectType"/>별 <see cref="IEffectHandler"/>를 찾아 실행하는 유일한 진입점. 파이프라인·외부 코드는 핸들러를 직접 호출하지 않는다 (EffectSystem.md §2).
    /// </summary>
    public class EffectSystem : IEffectSystem
    {
        private readonly Dictionary<EffectType, IEffectHandler> _handlers = new();

        /// <summary>런타임에 핸들러를 등록한다. 동일 <see cref="EffectType"/>은 덮어쓴다.</summary>
        public void Register(IEffectHandler handler) => _handlers[handler.EffectType] = handler;

        /// <summary>
        /// 스펙에 맞는 핸들러를 실행한다. 등록되지 않은 <see cref="EffectType"/>은 무시된다.
        /// </summary>
        public void Execute(EffectSpec spec, EffectContext ctx)
        {
            if (spec == null || ctx == null)
                return;

            if (!_handlers.TryGetValue(spec.EffectType, out var handler))
                return;

            ctx.CurrentEffect = spec;
            if (!handler.CanExecute(ctx, spec.Params))
                return;

            handler.Execute(ctx, spec.Params);
        }

        /// <summary>테스트·프로토타입용: DamageAdd, PelletAdd, DelayReduce, DelayRemove 핸들러가 등록된 인스턴스.</summary>
        public static EffectSystem CreateWithDefaultHandlers()
        {
            var sys = new EffectSystem();
            sys.Register(new DamageAddHandler());
            sys.Register(new PelletAddHandler());
            sys.Register(new DelayReduceHandler());
            sys.Register(new DelayRemoveHandler());
            return sys;
        }
    }
}
