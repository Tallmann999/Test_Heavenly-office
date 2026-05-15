namespace HeavenOffice
{
    public sealed class DocumentDecisionEvaluator
    {
        public string GetExpectedStampId(SoulDocument document)
        {
            return document.ExpectedStampId;
        }

        public HeavenOfficeMistakeReason GetMistakeReason(SoulDocument document, string selectedStampId)
        {
            return selectedStampId == GetExpectedStampId(document)
                ? HeavenOfficeMistakeReason.None
                : HeavenOfficeMistakeReason.WrongStamp;
        }
    }
}
