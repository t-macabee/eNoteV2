import 'package:enote_core/enote_core.dart';

const String rentalDebtNotice =
    'Imate neizmireno dugovanje od prethodnog iznajmljivanja. '
    'Izmirite ga prije novog zahtjeva.';

String membershipNotice(DateTime? paidUntil) => paidUntil == null
    ? 'Članarina nije aktivna.'
    : 'Članarina je istekla ${formatDate(paidUntil)} '
          'Obratite se školi za obnovu.';

String tuitionDueNotice(DateTime? paidUntil) => paidUntil == null
    ? 'Školarina nije plaćena.'
    : 'Školarina je istekla ${formatDate(paidUntil)}';
