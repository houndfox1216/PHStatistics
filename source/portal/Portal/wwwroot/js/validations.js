const countryCodes = '0123456789ABCDEFGHJKLMNPQRSTUVXYWZIO';
const weights = [1, 9, 8, 7, 6, 5, 4, 3, 2, 1, 1]

function isTaiwanIdNo(id) {
  if (!/^([A-Z])[1,2]\d{8}$/.test(id)) return false
  const codes = `${countryCodes.indexOf(id[0])}${id.substring(1)}`;
  const value = [...codes].reduce((prev, curr, idx) => prev + curr * weights[idx], 0);
  return (value % 10) === 0;
}

function isNewTaiwanResidentCertificateNo(id) {
  if (!/^([A-Z])[8,9]\d{8}$/.test(id)) return false
  const codes = `${countryCodes.indexOf(id[0])}${id.substring(1)}`;
  const value = [...codes].reduce((prev, curr, idx) => prev + curr * weights[idx], 0);
  return (value % 10) === 0;
}

function isOldTaiwanResidentCertificateNo(id) {
  if (!/^([A-Z])([A-D])\d{8}$/.test(id)) return false
  const codes = `${countryCodes.indexOf(id[0])}${countryCodes.indexOf(id[1]) % 10}${id.substring(2)}`;
  const value = [...codes].reduce((prev, curr, idx) => prev + curr * weights[idx], 0);
  return (value % 10) === 0;
}

function isTaiwanResidentCertificateNo(id) {
  return isNewTaiwanResidentCertificateNo(id) || isOldTaiwanResidentCertificateNo(id)
}
