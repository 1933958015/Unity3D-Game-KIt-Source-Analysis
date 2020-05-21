using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit3D
{
    public class ChomperSMBReturn : SceneLinkedSMB<ChomperBehavior>//咀嚼者回到初始点的情况
    {
        public override void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)//开始时怪物处于初始点
        {
            m_MonoBehaviour.WalkBackToBase();//返回初始位置
        }

        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)//怪物每帧的运动模式
        {
            base.OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);//当动画机状态为未转换状态时，每帧调用一次   

            m_MonoBehaviour.FindTarget();//找到目标记录位置

            if(m_MonoBehaviour.target != null)//如果发现目标开始前往目标点
                m_MonoBehaviour.StartPursuit(); // if the player got back in our vision range, resume pursuit!
            else //否则返回初始位置
                m_MonoBehaviour.WalkBackToBase();
        }
    }
}