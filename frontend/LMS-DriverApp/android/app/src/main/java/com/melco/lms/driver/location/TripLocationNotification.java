package com.melco.lms.driver.location;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.os.Build;

import com.melco.lms.driver.MainActivity;
import com.melco.lms.driver.R;

/** Persistent "trip in progress" notification required by the location foreground service. */
final class TripLocationNotification {
    static final int NOTIFICATION_ID = 3101;
    private static final String CHANNEL_ID = "lms_trip_location";

    private TripLocationNotification() {
    }

    static Notification build(Context context) {
        Notification.Builder builder = Build.VERSION.SDK_INT >= Build.VERSION_CODES.O
                ? new Notification.Builder(context, ensureChannel(context))
                : new Notification.Builder(context);
        return builder
                .setSmallIcon(android.R.drawable.ic_menu_mylocation)
                .setContentTitle(context.getString(R.string.trip_location_notification_title))
                .setContentText(context.getString(R.string.trip_location_notification_text))
                .setContentIntent(openApp(context))
                .setOngoing(true)
                .setShowWhen(false)
                .build();
    }

    private static String ensureChannel(Context context) {
        NotificationManager manager = context.getSystemService(NotificationManager.class);
        NotificationChannel channel = new NotificationChannel(CHANNEL_ID,
                context.getString(R.string.trip_location_channel_name), NotificationManager.IMPORTANCE_LOW);
        channel.setShowBadge(false);
        manager.createNotificationChannel(channel);
        return CHANNEL_ID;
    }

    private static PendingIntent openApp(Context context) {
        Intent intent = new Intent(context, MainActivity.class)
                .setFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        return PendingIntent.getActivity(context, 0, intent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
    }
}
