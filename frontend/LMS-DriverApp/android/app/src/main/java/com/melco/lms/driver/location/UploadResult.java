package com.melco.lms.driver.location;

/** Result of one upload attempt: the outcome plus acceptedSeq or the server's Retry-After hint. */
public final class UploadResult {
    public final UploadOutcome outcome;
    public final long acceptedSeq;
    public final int retryAfterSeconds;

    private UploadResult(UploadOutcome outcome, long acceptedSeq, int retryAfterSeconds) {
        this.outcome = outcome;
        this.acceptedSeq = acceptedSeq;
        this.retryAfterSeconds = retryAfterSeconds;
    }

    public static UploadResult acknowledged(long acceptedSeq) {
        return new UploadResult(UploadOutcome.ACKNOWLEDGED, acceptedSeq, 0);
    }

    public static UploadResult retryLater(int retryAfterSeconds) {
        return new UploadResult(UploadOutcome.RETRY_LATER, 0, retryAfterSeconds);
    }

    public static UploadResult of(UploadOutcome outcome) {
        return new UploadResult(outcome, 0, 0);
    }
}
