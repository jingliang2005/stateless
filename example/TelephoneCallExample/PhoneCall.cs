using System;
using Stateless;
using Stateless.Graph;

namespace TelephoneCallExample
{
    /// <summary>
    /// �绰
    /// </summary>
    public class PhoneCall
    {
        /// <summary>
        /// ��������
        /// </summary>
        enum Trigger
        {
            /// <summary>
            /// ����绰��
            /// </summary>
            CallDialed,
            /// <summary>
            /// ���������ӣ�
            /// </summary>
            CallConnected,
            /// <summary>
            /// ���ԣ�
            /// </summary>
            LeftMessage,
            /// <summary>
            /// ���ڱ���״̬��
            /// </summary>
            PlacedOnHold,
            /// <summary>
            /// ȡ��������
            /// </summary>
            TakenOffHold,
            /// <summary>
            /// �ֻ���ǽ���ӣ�
            /// </summary>
            PhoneHurledAgainstWall,
            /// <summary>
            /// ��˷羲��
            /// </summary>
            MuteMicrophone,
            /// <summary>
            /// ȡ��������˷磬
            /// </summary>
            UnmuteMicrophone,
            /// <summary>
            /// �趨����
            /// </summary>
            SetVolume
        }

        /// <summary>
        /// ״̬
        /// </summary>
        enum State
        {
            /// <summary>
            /// ժ�����绰�Ҷϣ�����������
            /// </summary>
            OffHook,
            /// <summary>
            /// ����
            /// </summary>
            Ringing,
            /// <summary>
            /// ������
            /// </summary>
            Connected,
            /// <summary>
            /// ����
            /// </summary>
            OnHold,
            /// <summary>
            /// �绰������
            /// </summary>
            PhoneDestroyed
        }
        
        /// <summary>
        /// ״̬,��ʼΪ�ػ���
        /// </summary>
        State _state = State.OffHook;
        
        /// <summary>
        /// ״̬����
        /// </summary>
        StateMachine<State, Trigger> _machine;

        /// <summary>
        /// ״̬����������������������������
        /// </summary>
        StateMachine<State, Trigger>.TriggerWithParameters<int> _setVolumeTrigger;

        /// <summary>
        /// ���ñ��д�������״̬����������������
        /// </summary>
        StateMachine<State, Trigger>.TriggerWithParameters<string> _setCalleeTrigger;

        /// <summary>
        /// ������
        /// </summary>
        string _caller;
        /// <summary>
        /// ������
        /// </summary>
        string _callee;
        /// <summary>
        /// �绰
        /// </summary>
        /// <param name="caller">������</param>
        public PhoneCall(string caller)
        { 
            _caller = caller;
            _machine = new StateMachine<State, Trigger>(() => _state, s => _state = s);

            _setVolumeTrigger = _machine.SetTriggerParameters<int>(Trigger.SetVolume);
            _setCalleeTrigger = _machine.SetTriggerParameters<string>(Trigger.CallDialed);

            // ��ժ��״̬������в���绰����������塣
            _machine.Configure(State.OffHook)
	            .Permit(Trigger.CallDialed, State.Ringing);

            // ������״̬���Ƚ��в��Ų���������������Ӵ��������������������״̬��
            _machine.Configure(State.Ringing)
                .OnEntryFrom(_setCalleeTrigger, callee => OnDialed(callee), "Caller number to call")
	            .Permit(Trigger.CallConnected, State.Connected);

            // ����״̬����������״̬��ʼ��ʱ���˳�����״ֹ̬ͣ��ʱ��
            // �������ڲ�״̬��������ȡ������������������
            // ������״̬���һ������ڱ���״̬�������ֱ�ת����ժ��״̬�ͱ���״̬��
            _machine.Configure(State.Connected)
                .OnEntry(t => StartCallTimer())
                .OnExit(t => StopCallTimer())
                .InternalTransition(Trigger.MuteMicrophone, t => OnMute())
                .InternalTransition(Trigger.UnmuteMicrophone, t => OnUnmute())
                .InternalTransition<int>(_setVolumeTrigger, (volume, t) => OnSetVolume(volume))
                .Permit(Trigger.LeftMessage, State.OffHook)
	            .Permit(Trigger.PlacedOnHold, State.OnHold);

            // �ڱ���״̬������״̬�����ӡ���ȡ�����������ֻ��������ֱ��л�������״̬���ֻ���״̬��
            _machine.Configure(State.OnHold)
                .SubstateOf(State.Connected)
                .Permit(Trigger.TakenOffHold, State.Connected)
                .Permit(Trigger.PhoneHurledAgainstWall, State.PhoneDestroyed);

            // ע��һ���ص����ûص�����ÿ��״̬����һ��״̬ת��Ϊ��һ��״̬ʱ���á�
            _machine.OnTransitioned(t => Console.WriteLine($"OnTransitioned: {t.Source} -> {t.Destination} via {t.Trigger}({string.Join(", ",  t.Parameters)})"));
        }
      
        /// <summary>
        /// �趨����
        /// </summary>
        /// <param name="volume"></param>
        void OnSetVolume(int volume)
        {
            Console.WriteLine("Volume set to " + volume + "!");
        }
        /// <summary>
        /// ȡ������
        /// </summary>
        void OnUnmute()
        {
            Console.WriteLine("Microphone unmuted!");
        }
        /// <summary>
        /// ����
        /// </summary>
        void OnMute()
        {
            Console.WriteLine("Microphone muted!");
        }
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="callee"></param>
        void OnDialed(string callee)
        {
            _callee = callee;
            Console.WriteLine("[Phone Call] placed for : [{0}]", _callee);
        }
        /// <summary>
        /// ��ʼͨ����ʱ��
        /// </summary>
        void StartCallTimer()
        {
            Console.WriteLine("[Timer:] Call started at {0}", DateTime.Now);
        }
        /// <summary>
        /// ֹͣͨ����ʱ��
        /// </summary>
        void StopCallTimer()
        {
            Console.WriteLine("[Timer:] Call ended at {0}", DateTime.Now);
        }
        /// <summary>
        /// ����
        /// </summary>
        public void Mute()
        {
            _machine.Fire(Trigger.MuteMicrophone);
        }
        /// <summary>
        /// ȡ������
        /// </summary>
        public void Unmute()
        {
            _machine.Fire(Trigger.UnmuteMicrophone);
        }
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="volume"></param>
        public void SetVolume(int volume)
        {
            _machine.Fire(_setVolumeTrigger, volume);
        }
        /// <summary>
        /// ��ӡ
        /// </summary>
        public void Print()
        {
            Console.WriteLine("[{1}] placed call and [Status:] {0}", _machine.State, _caller);
        }
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="callee"></param>
        public void Dialed(string callee)
        {           
            _machine.Fire(_setCalleeTrigger, callee);
        }
        /// <summary>
        /// ����
        /// </summary>
        public void Connected()
        {
            _machine.Fire(Trigger.CallConnected);
        }
        /// <summary>
        /// ����
        /// </summary>
        public void Hold()
        {
            _machine.Fire(Trigger.PlacedOnHold);
        }
        /// <summary>
        /// �ָ�
        /// </summary>
        public void Resume()
        {
            _machine.Fire(Trigger.TakenOffHold);
        }
        /// <summary>
        /// ��ͼ
        /// </summary>
        /// <returns></returns>
        public string ToDotGraph()
        {
            return UmlDotGraph.Format(_machine.GetInfo());
        }
  
    
    
    
    
    
    }
}