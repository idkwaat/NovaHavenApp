import 'package:flutter/material.dart';

/// Shared native palette adapted from design-system/nova-haven/MASTER.md.
abstract final class NovaPalette {
  static const soil = Color(0xFF211D19);
  static const paper = Color(0xFF30271F);
  static const raisedPaper = Color(0xFF3B3026);
  static const cream = Color(0xFFEFE2CA);
  static const mutedCream = Color(0xFFD4C3A7);
  static const olive = Color(0xFF9A9B70);
  static const teal = Color(0xFF91A499);
  static const timber = Color(0xFF80664A);
  static const harvest = Color(0xFFD1AE71);
  static const rust = Color(0xFFBF816A);
}

abstract final class NovaTheme {
  static ThemeData get dark {
    const scheme = ColorScheme(
      brightness: Brightness.dark,
      primary: NovaPalette.olive,
      onPrimary: NovaPalette.soil,
      secondary: NovaPalette.teal,
      onSecondary: NovaPalette.soil,
      error: NovaPalette.rust,
      onError: NovaPalette.soil,
      surface: NovaPalette.paper,
      onSurface: NovaPalette.cream,
      surfaceContainerHighest: NovaPalette.raisedPaper,
      onSurfaceVariant: NovaPalette.mutedCream,
      outline: NovaPalette.timber,
      outlineVariant: Color(0xFF5F4C39),
      shadow: Colors.black,
      scrim: Colors.black,
      inverseSurface: NovaPalette.cream,
      onInverseSurface: NovaPalette.soil,
      inversePrimary: Color(0xFF626344),
      surfaceTint: NovaPalette.olive,
    );

    final base = ThemeData(useMaterial3: true, colorScheme: scheme);
    final text = base.textTheme.copyWith(
      headlineLarge: base.textTheme.headlineLarge?.copyWith(
        fontFamily: 'serif',
        fontSize: 32,
        height: 1.2,
        fontWeight: FontWeight.w700,
        color: NovaPalette.cream,
      ),
      headlineMedium: base.textTheme.headlineMedium?.copyWith(
        fontFamily: 'serif',
        fontSize: 25,
        height: 1.24,
        fontWeight: FontWeight.w700,
        color: NovaPalette.cream,
      ),
      headlineSmall: base.textTheme.headlineSmall?.copyWith(
        fontFamily: 'serif',
        fontSize: 24,
        height: 1.25,
        fontWeight: FontWeight.w700,
        color: NovaPalette.cream,
      ),
      titleLarge: base.textTheme.titleLarge?.copyWith(
        fontFamily: 'serif',
        fontSize: 20,
        height: 1.3,
        fontWeight: FontWeight.w700,
        color: NovaPalette.cream,
      ),
      bodyLarge: base.textTheme.bodyLarge?.copyWith(
        fontSize: 16,
        height: 1.55,
        color: NovaPalette.cream,
      ),
      bodyMedium: base.textTheme.bodyMedium?.copyWith(
        fontSize: 14,
        height: 1.5,
        color: NovaPalette.mutedCream,
      ),
      labelLarge: base.textTheme.labelLarge?.copyWith(
        fontSize: 14,
        fontWeight: FontWeight.w600,
      ),
    );

    return base.copyWith(
      scaffoldBackgroundColor: NovaPalette.soil,
      textTheme: text,
      appBarTheme: AppBarTheme(
        backgroundColor: NovaPalette.soil,
        foregroundColor: NovaPalette.cream,
        elevation: 0,
        scrolledUnderElevation: 0,
        titleTextStyle: text.titleLarge,
        iconTheme: const IconThemeData(color: NovaPalette.mutedCream, size: 23),
      ),
      cardTheme: CardThemeData(
        color: NovaPalette.raisedPaper,
        elevation: 0,
        margin: const EdgeInsets.symmetric(vertical: 6),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: NovaPalette.timber, width: 1),
        ),
      ),
      listTileTheme: const ListTileThemeData(
        iconColor: NovaPalette.olive,
        textColor: NovaPalette.cream,
        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        minVerticalPadding: 12,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: NovaPalette.paper,
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        labelStyle: const TextStyle(color: NovaPalette.mutedCream),
        hintStyle: const TextStyle(color: NovaPalette.mutedCream),
        prefixIconColor: NovaPalette.olive,
        suffixIconColor: NovaPalette.harvest,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: NovaPalette.timber),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: NovaPalette.timber),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: NovaPalette.olive, width: 2),
        ),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: NovaPalette.paper,
        indicatorColor: NovaPalette.olive,
        elevation: 0,
        labelTextStyle: WidgetStateProperty.resolveWith((states) => TextStyle(
              color: states.contains(WidgetState.selected)
                  ? NovaPalette.cream
                  : NovaPalette.mutedCream,
              fontSize: 12,
              fontWeight: states.contains(WidgetState.selected)
                  ? FontWeight.w700
                  : FontWeight.w500,
            )),
      ),
      chipTheme: base.chipTheme.copyWith(
        backgroundColor: NovaPalette.paper,
        selectedColor: NovaPalette.olive,
        side: const BorderSide(color: NovaPalette.timber),
        labelStyle: const TextStyle(color: NovaPalette.cream, fontSize: 14),
        secondaryLabelStyle:
            const TextStyle(color: NovaPalette.soil, fontSize: 14),
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
      ),
      dividerTheme: const DividerThemeData(
        color: NovaPalette.timber,
        thickness: 1,
        space: 32,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(48, 48),
          backgroundColor: NovaPalette.olive,
          foregroundColor: NovaPalette.soil,
          textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(48, 48),
          foregroundColor: NovaPalette.cream,
          side: const BorderSide(color: NovaPalette.timber),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(9)),
        ),
      ),
      iconButtonTheme: IconButtonThemeData(
        style: IconButton.styleFrom(
          minimumSize: const Size(48, 48),
          foregroundColor: NovaPalette.mutedCream,
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: NovaPalette.raisedPaper,
        contentTextStyle: text.bodyMedium?.copyWith(color: NovaPalette.cream),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }
}
