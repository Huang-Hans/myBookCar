namespace LMS.Dispatch.Domain.Reports;

// 事故报告的处理状态。RECORDED 只表示本系统已保存；FORWARDED（已转交运营渠道）与 ACKNOWLEDGED（运营已签收）
// 各自对应真实事实，目前没有运营接入，因此只会出现 RECORDED。保存、转交、签收和拨号接通是不同的事实，不互相推定。
public enum IncidentStatus
{
    Recorded,
    Forwarded,
    Acknowledged
}
