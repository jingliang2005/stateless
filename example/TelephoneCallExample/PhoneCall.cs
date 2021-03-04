using System;
using Stateless;
using Stateless.Graph;

namespace TelephoneCallExample
{
    /// <summary>
    /// 电话
    /// </summary>
    public class PhoneCall
    {
        /// <summary>
        /// 触发器。
        /// </summary>
        enum Trigger
        {
            /// <summary>
            /// 拨打电话。
            /// </summary>
            CallDialed,
            /// <summary>
            /// 呼叫已连接，
            /// </summary>
            CallConnected,
            /// <summary>
            /// 留言，
            /// </summary>
            LeftMessage,
            /// <summary>
            /// 置于保留状态，
            /// </summary>
            PlacedOnHold,
            /// <summary>
            /// 取消保留，
            /// </summary>
            TakenOffHold,
            /// <summary>
            /// 手机向墙上扔，
            /// </summary>
            PhoneHurledAgainstWall,
            /// <summary>
            /// 麦克风静音
            /// </summary>
            MuteMicrophone,
            /// <summary>
            /// 取消静音麦克风，
            /// </summary>
            UnmuteMicrophone,
            /// <summary>
            /// 设定音量
            /// </summary>
            SetVolume
        }

        /// <summary>
        /// 状态
        /// </summary>
        enum State
        {
            /// <summary>
            /// 摘机（电话挂断？待机？）。
            /// </summary>
            OffHook,
            /// <summary>
            /// 响铃
            /// </summary>
            Ringing,
            /// <summary>
            /// 已连接
            /// </summary>
            Connected,
            /// <summary>
            /// 保留
            /// </summary>
            OnHold,
            /// <summary>
            /// 电话被销毁
            /// </summary>
            PhoneDestroyed
        }
        
        /// <summary>
        /// 状态,初始为关机。
        /// </summary>
        State _state = State.OffHook;
        
        /// <summary>
        /// 状态机。
        /// </summary>
        StateMachine<State, Trigger> _machine;

        /// <summary>
        /// 状态机，带参数触发。设置音量触发
        /// </summary>
        StateMachine<State, Trigger>.TriggerWithParameters<int> _setVolumeTrigger;

        /// <summary>
        /// 设置被叫触发器，状态机，带参数触发。
        /// </summary>
        StateMachine<State, Trigger>.TriggerWithParameters<string> _setCalleeTrigger;

        /// <summary>
        /// 呼叫者
        /// </summary>
        string _caller;
        /// <summary>
        /// 被叫者
        /// </summary>
        string _callee;
        /// <summary>
        /// 电话
        /// </summary>
        /// <param name="caller">呼叫者</param>
        public PhoneCall(string caller)
        { 
            _caller = caller;
            _machine = new StateMachine<State, Trigger>(() => _state, s => _state = s);

            _setVolumeTrigger = _machine.SetTriggerParameters<int>(Trigger.SetVolume);
            _setCalleeTrigger = _machine.SetTriggerParameters<string>(Trigger.CallDialed);

            // 在摘机状态，如果有拨打电话，则进入响铃。
            _machine.Configure(State.OffHook)
	            .Permit(Trigger.CallDialed, State.Ringing);

            // 在响铃状态，先进行拨号操作，如果呼叫连接触发器触发则进入已连接状态。
            _machine.Configure(State.Ringing)
                .OnEntryFrom(_setCalleeTrigger, callee => OnDialed(callee), "Caller number to call")
	            .Permit(Trigger.CallConnected, State.Connected);

            // 连接状态，进入连接状态开始计时，退出连接状态停止计时。
            // 并且有内部状态，静音，取消静音，设置音量。
            // 在连接状态，挂机和置于保留状态触发器分别转换到摘机状态和保留状态。
            _machine.Configure(State.Connected)
                .OnEntry(t => StartCallTimer())
                .OnExit(t => StopCallTimer())
                .InternalTransition(Trigger.MuteMicrophone, t => OnMute())
                .InternalTransition(Trigger.UnmuteMicrophone, t => OnUnmute())
                .InternalTransition<int>(_setVolumeTrigger, (volume, t) => OnSetVolume(volume))
                .Permit(Trigger.LeftMessage, State.OffHook)
	            .Permit(Trigger.PlacedOnHold, State.OnHold);

            // 在保留状态，有子状态已连接。当取消保留和扔手机触发器分别切换到连接状态和手机损坏状态。
            _machine.Configure(State.OnHold)
                .SubstateOf(State.Connected)
                .Permit(Trigger.TakenOffHold, State.Connected)
                .Permit(Trigger.PhoneHurledAgainstWall, State.PhoneDestroyed);

            // 注册一个回调，该回调将在每次状态机从一种状态转换为另一种状态时调用。
            _machine.OnTransitioned(t => Console.WriteLine($"OnTransitioned: {t.Source} -> {t.Destination} via {t.Trigger}({string.Join(", ",  t.Parameters)})"));
        }
      
        /// <summary>
        /// 设定音量
        /// </summary>
        /// <param name="volume"></param>
        void OnSetVolume(int volume)
        {
            Console.WriteLine("Volume set to " + volume + "!");
        }
        /// <summary>
        /// 取消静音
        /// </summary>
        void OnUnmute()
        {
            Console.WriteLine("Microphone unmuted!");
        }
        /// <summary>
        /// 静音
        /// </summary>
        void OnMute()
        {
            Console.WriteLine("Microphone muted!");
        }
        /// <summary>
        /// 拨号
        /// </summary>
        /// <param name="callee"></param>
        void OnDialed(string callee)
        {
            _callee = callee;
            Console.WriteLine("[Phone Call] placed for : [{0}]", _callee);
        }
        /// <summary>
        /// 开始通话计时器
        /// </summary>
        void StartCallTimer()
        {
            Console.WriteLine("[Timer:] Call started at {0}", DateTime.Now);
        }
        /// <summary>
        /// 停止通话计时器
        /// </summary>
        void StopCallTimer()
        {
            Console.WriteLine("[Timer:] Call ended at {0}", DateTime.Now);
        }
        /// <summary>
        /// 静音
        /// </summary>
        public void Mute()
        {
            _machine.Fire(Trigger.MuteMicrophone);
        }
        /// <summary>
        /// 取消静音
        /// </summary>
        public void Unmute()
        {
            _machine.Fire(Trigger.UnmuteMicrophone);
        }
        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume"></param>
        public void SetVolume(int volume)
        {
            _machine.Fire(_setVolumeTrigger, volume);
        }
        /// <summary>
        /// 打印
        /// </summary>
        public void Print()
        {
            Console.WriteLine("[{1}] placed call and [Status:] {0}", _machine.State, _caller);
        }
        /// <summary>
        /// 拨号
        /// </summary>
        /// <param name="callee"></param>
        public void Dialed(string callee)
        {           
            _machine.Fire(_setCalleeTrigger, callee);
        }
        /// <summary>
        /// 连接
        /// </summary>
        public void Connected()
        {
            _machine.Fire(Trigger.CallConnected);
        }
        /// <summary>
        /// 保留
        /// </summary>
        public void Hold()
        {
            _machine.Fire(Trigger.PlacedOnHold);
        }
        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            _machine.Fire(Trigger.TakenOffHold);
        }
        /// <summary>
        /// 点图
        /// </summary>
        /// <returns></returns>
        public string ToDotGraph()
        {
            return UmlDotGraph.Format(_machine.GetInfo());
        }
  
    
    
    
    
    
    }
}