using System;
using UnityEngine;

namespace WhaleShark.Core
{
    /// <summary>
    /// 게임 전역 이벤트 버스 시스템
    /// 컴포넌트 간 느슨한 결합을 위한 중앙 집중식 이벤트 관리
    /// 게임 상태 변화, UI 업데이트, 사운드 재생 등의 이벤트를 관리합니다
    /// </summary>
    public static class EventBus
    {
        /// <summary>피격 시 발생하는 이벤트 (카메라 쉐이크, 이펙트 재생 등에 사용)</summary>
        public static event Action<Vector3> Hit;
        /// <summary>피격 이벤트를 발생시킵니다</summary>
        /// <param name="pos">피격 위치</param>
        public static void RaiseHit(Vector3 pos) => Hit?.Invoke(pos);

        /// <summary>게임 일시정지 상태가 변경되었을 때 발생하는 이벤트</summary>
        public static event Action<bool> PauseToggled;
        /// <summary>일시정지 상태 변경 이벤트를 발생시킵니다</summary>
        /// <param name="on">true: 일시정지, false: 재개</param>
        public static void PublishPause(bool on) => PauseToggled?.Invoke(on);

        /// <summary>플레이어 사망 시 발생하는 이벤트</summary>
        public static event Action PlayerDied;
        /// <summary>플레이어 사망 이벤트를 발생시킵니다</summary>
        public static void PublishPlayerDied() => PlayerDied?.Invoke();
        
    }
}