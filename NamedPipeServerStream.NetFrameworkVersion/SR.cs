// Decompiled with JetBrains decompiler
// Type: System.SR
// Assembly: System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 95B4B630-A4AF-4E9A-8336-D9E36310DACA
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Core.dll

using System.Globalization;
using System.Resources;
using System.Threading;

namespace System
{
    internal sealed class SR
    {
        internal const string ArgumentOutOfRange_NeedNonNegNum = "ArgumentOutOfRange_NeedNonNegNum";
        internal const string Argument_WrongAsyncResult = "Argument_WrongAsyncResult";
        internal const string Argument_InvalidOffLen = "Argument_InvalidOffLen";
        internal const string Argument_NeedNonemptyPipeName = "Argument_NeedNonemptyPipeName";
        internal const string Argument_EmptyServerName = "Argument_EmptyServerName";
        internal const string Argument_NonContainerInvalidAnyFlag = "Argument_NonContainerInvalidAnyFlag";
        internal const string Argument_InvalidHandle = "Argument_InvalidHandle";
        internal const string ArgumentNull_Buffer = "ArgumentNull_Buffer";
        internal const string ArgumentNull_ServerName = "ArgumentNull_ServerName";
        internal const string ArgumentOutOfRange_AdditionalAccessLimited = "ArgumentOutOfRange_AdditionalAccessLimited";
        internal const string ArgumentOutOfRange_AnonymousReserved = "ArgumentOutOfRange_AnonymousReserved";
        internal const string ArgumentOutOfRange_TransmissionModeByteOrMsg = "ArgumentOutOfRange_TransmissionModeByteOrMsg";
        internal const string ArgumentOutOfRange_DirectionModeInOrOut = "ArgumentOutOfRange_DirectionModeInOrOut";
        internal const string ArgumentOutOfRange_DirectionModeInOutOrInOut = "ArgumentOutOfRange_DirectionModeInOutOrInOut";
        internal const string ArgumentOutOfRange_ImpersonationInvalid = "ArgumentOutOfRange_ImpersonationInvalid";
        internal const string ArgumentOutOfRange_ImpersonationOptionsInvalid = "ArgumentOutOfRange_ImpersonationOptionsInvalid";
        internal const string ArgumentOutOfRange_OptionsInvalid = "ArgumentOutOfRange_OptionsInvalid";
        internal const string ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable = "ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable";
        internal const string ArgumentOutOfRange_InvalidPipeAccessRights = "ArgumentOutOfRange_InvalidPipeAccessRights";
        internal const string ArgumentOutOfRange_InvalidTimeout = "ArgumentOutOfRange_InvalidTimeout";
        internal const string ArgumentOutOfRange_MaxNumServerInstances = "ArgumentOutOfRange_MaxNumServerInstances";
        internal const string ArgumentOutOfRange_NeedValidPipeAccessRights = "ArgumentOutOfRange_NeedValidPipeAccessRights";
        internal const string IndexOutOfRange_IORaceCondition = "IndexOutOfRange_IORaceCondition";
        internal const string InvalidOperation_EndReadCalledMultiple = "InvalidOperation_EndReadCalledMultiple";
        internal const string InvalidOperation_EndWriteCalledMultiple = "InvalidOperation_EndWriteCalledMultiple";
        internal const string InvalidOperation_EndWaitForConnectionCalledMultiple = "InvalidOperation_EndWaitForConnectionCalledMultiple";
        internal const string InvalidOperation_PipeNotYetConnected = "InvalidOperation_PipeNotYetConnected";
        internal const string InvalidOperation_PipeDisconnected = "InvalidOperation_PipeDisconnected";
        internal const string InvalidOperation_PipeHandleNotSet = "InvalidOperation_PipeHandleNotSet";
        internal const string InvalidOperation_PipeNotAsync = "InvalidOperation_PipeNotAsync";
        internal const string InvalidOperation_PipeReadModeNotMessage = "InvalidOperation_PipeReadModeNotMessage";
        internal const string InvalidOperation_PipeMessageTypeNotSupported = "InvalidOperation_PipeMessageTypeNotSupported";
        internal const string InvalidOperation_PipeAlreadyConnected = "InvalidOperation_PipeAlreadyConnected";
        internal const string InvalidOperation_PipeAlreadyDisconnected = "InvalidOperation_PipeAlreadyDisconnected";
        internal const string InvalidOperation_PipeClosed = "InvalidOperation_PipeClosed";
        internal const string IO_FileTooLongOrHandleNotSync = "IO_FileTooLongOrHandleNotSync";
        internal const string IO_EOF_ReadBeyondEOF = "IO_EOF_ReadBeyondEOF";
        internal const string IO_FileNotFound = "IO_FileNotFound";
        internal const string IO_FileNotFound_FileName = "IO_FileNotFound_FileName";
        internal const string IO_IO_AlreadyExists_Name = "IO_IO_AlreadyExists_Name";
        internal const string IO_IO_BindHandleFailed = "IO_IO_BindHandleFailed";
        internal const string IO_IO_FileExists_Name = "IO_IO_FileExists_Name";
        internal const string IO_IO_NoPermissionToDirectoryName = "IO_IO_NoPermissionToDirectoryName";
        internal const string IO_IO_SharingViolation_File = "IO_IO_SharingViolation_File";
        internal const string IO_IO_SharingViolation_NoFileName = "IO_IO_SharingViolation_NoFileName";
        internal const string IO_IO_PipeBroken = "IO_IO_PipeBroken";
        internal const string IO_IO_InvalidPipeHandle = "IO_IO_InvalidPipeHandle";
        internal const string IO_OperationAborted = "IO_OperationAborted";
        internal const string IO_DriveNotFound_Drive = "IO_DriveNotFound_Drive";
        internal const string IO_PathNotFound_Path = "IO_PathNotFound_Path";
        internal const string IO_PathNotFound_NoPathName = "IO_PathNotFound_NoPathName";
        internal const string IO_PathTooLong = "IO_PathTooLong";
        internal const string NotSupported_IONonFileDevices = "NotSupported_IONonFileDevices";
        internal const string NotSupported_MemStreamNotExpandable = "NotSupported_MemStreamNotExpandable";
        internal const string NotSupported_UnreadableStream = "NotSupported_UnreadableStream";
        internal const string NotSupported_UnseekableStream = "NotSupported_UnseekableStream";
        internal const string NotSupported_UnwritableStream = "NotSupported_UnwritableStream";
        internal const string NotSupported_AnonymousPipeUnidirectional = "NotSupported_AnonymousPipeUnidirectional";
        internal const string NotSupported_AnonymousPipeMessagesNotSupported = "NotSupported_AnonymousPipeMessagesNotSupported";
        internal const string ObjectDisposed_FileClosed = "ObjectDisposed_FileClosed";
        internal const string ObjectDisposed_PipeClosed = "ObjectDisposed_PipeClosed";
        internal const string ObjectDisposed_ReaderClosed = "ObjectDisposed_ReaderClosed";
        internal const string ObjectDisposed_StreamClosed = "ObjectDisposed_StreamClosed";
        internal const string ObjectDisposed_WriterClosed = "ObjectDisposed_WriterClosed";
        internal const string PlatformNotSupported_NamedPipeServers = "PlatformNotSupported_NamedPipeServers";
        internal const string UnauthorizedAccess_IODenied_Path = "UnauthorizedAccess_IODenied_Path";
        internal const string UnauthorizedAccess_IODenied_NoPathName = "UnauthorizedAccess_IODenied_NoPathName";
        internal const string TraceAsTraceSource = "TraceAsTraceSource";
        internal const string ArgumentOutOfRange_NeedValidLogRetention = "ArgumentOutOfRange_NeedValidLogRetention";
        internal const string ArgumentOutOfRange_NeedMaxFileSizeGEBufferSize = "ArgumentOutOfRange_NeedMaxFileSizeGEBufferSize";
        internal const string ArgumentOutOfRange_NeedValidMaxNumFiles = "ArgumentOutOfRange_NeedValidMaxNumFiles";
        internal const string ArgumentOutOfRange_NeedValidId = "ArgumentOutOfRange_NeedValidId";
        internal const string ArgumentOutOfRange_MaxArgExceeded = "ArgumentOutOfRange_MaxArgExceeded";
        internal const string ArgumentOutOfRange_MaxStringsExceeded = "ArgumentOutOfRange_MaxStringsExceeded";
        internal const string NotSupported_DownLevelVista = "NotSupported_DownLevelVista";
        internal const string Argument_NeedNonemptyDelimiter = "Argument_NeedNonemptyDelimiter";
        internal const string NotSupported_SetTextWriter = "NotSupported_SetTextWriter";
        internal const string Perflib_PlatformNotSupported = "Perflib_PlatformNotSupported";
        internal const string Perflib_Argument_CounterSetAlreadyRegister = "Perflib_Argument_CounterSetAlreadyRegister";
        internal const string Perflib_Argument_InvalidCounterType = "Perflib_Argument_InvalidCounterType";
        internal const string Perflib_Argument_InvalidCounterSetInstanceType = "Perflib_Argument_InvalidCounterSetInstanceType";
        internal const string Perflib_Argument_InstanceAlreadyExists = "Perflib_Argument_InstanceAlreadyExists";
        internal const string Perflib_Argument_CounterAlreadyExists = "Perflib_Argument_CounterAlreadyExists";
        internal const string Perflib_Argument_CounterNameAlreadyExists = "Perflib_Argument_CounterNameAlreadyExists";
        internal const string Perflib_Argument_ProviderNotFound = "Perflib_Argument_ProviderNotFound";
        internal const string Perflib_Argument_InvalidInstance = "Perflib_Argument_InvalidInstance";
        internal const string Perflib_Argument_EmptyInstanceName = "Perflib_Argument_EmptyInstanceName";
        internal const string Perflib_Argument_EmptyCounterName = "Perflib_Argument_EmptyCounterName";
        internal const string Perflib_InsufficientMemory_InstanceCounterBlock = "Perflib_InsufficientMemory_InstanceCounterBlock";
        internal const string Perflib_InsufficientMemory_CounterSetTemplate = "Perflib_InsufficientMemory_CounterSetTemplate";
        internal const string Perflib_InvalidOperation_CounterRefValue = "Perflib_InvalidOperation_CounterRefValue";
        internal const string Perflib_InvalidOperation_CounterSetNotInstalled = "Perflib_InvalidOperation_CounterSetNotInstalled";
        internal const string Perflib_InvalidOperation_InstanceNotFound = "Perflib_InvalidOperation_InstanceNotFound";
        internal const string Perflib_InvalidOperation_AddCounterAfterInstance = "Perflib_InvalidOperation_AddCounterAfterInstance";
        internal const string Perflib_InvalidOperation_NoActiveProvider = "Perflib_InvalidOperation_NoActiveProvider";
        internal const string Perflib_InvalidOperation_CounterSetContainsNoCounter = "Perflib_InvalidOperation_CounterSetContainsNoCounter";
        internal const string Arg_ArrayPlusOffTooSmall = "Arg_ArrayPlusOffTooSmall";
        internal const string Arg_HSCapacityOverflow = "Arg_HSCapacityOverflow";
        internal const string InvalidOperation_EnumFailedVersion = "InvalidOperation_EnumFailedVersion";
        internal const string InvalidOperation_EnumOpCantHappen = "InvalidOperation_EnumOpCantHappen";
        internal const string Serialization_MissingKeys = "Serialization_MissingKeys";
        internal const string LockRecursionException_RecursiveReadNotAllowed = "LockRecursionException_RecursiveReadNotAllowed";
        internal const string LockRecursionException_RecursiveWriteNotAllowed = "LockRecursionException_RecursiveWriteNotAllowed";
        internal const string LockRecursionException_RecursiveUpgradeNotAllowed = "LockRecursionException_RecursiveUpgradeNotAllowed";
        internal const string LockRecursionException_ReadAfterWriteNotAllowed = "LockRecursionException_ReadAfterWriteNotAllowed";
        internal const string LockRecursionException_WriteAfterReadNotAllowed = "LockRecursionException_WriteAfterReadNotAllowed";
        internal const string LockRecursionException_UpgradeAfterReadNotAllowed = "LockRecursionException_UpgradeAfterReadNotAllowed";
        internal const string LockRecursionException_UpgradeAfterWriteNotAllowed = "LockRecursionException_UpgradeAfterWriteNotAllowed";
        internal const string SynchronizationLockException_MisMatchedRead = "SynchronizationLockException_MisMatchedRead";
        internal const string SynchronizationLockException_MisMatchedWrite = "SynchronizationLockException_MisMatchedWrite";
        internal const string SynchronizationLockException_MisMatchedUpgrade = "SynchronizationLockException_MisMatchedUpgrade";
        internal const string SynchronizationLockException_IncorrectDispose = "SynchronizationLockException_IncorrectDispose";
        internal const string Cryptography_ArgECDHKeySizeMismatch = "Cryptography_ArgECDHKeySizeMismatch";
        internal const string Cryptography_ArgECDHRequiresECDHKey = "Cryptography_ArgECDHRequiresECDHKey";
        internal const string Cryptography_ArgECDsaRequiresECDsaKey = "Cryptography_ArgECDsaRequiresECDsaKey";
        internal const string Cryptography_ArgExpectedECDiffieHellmanCngPublicKey = "Cryptography_ArgExpectedECDiffieHellmanCngPublicKey";
        internal const string Cryptography_ArgMustBeCngAlgorithm = "Cryptography_ArgMustBeCngAlgorithm";
        internal const string Cryptography_ArgMustBeCngAlgorithmGroup = "Cryptography_ArgMustBeCngAlgorithmGroup";
        internal const string Cryptography_ArgMustBeCngKeyBlobFormat = "Cryptography_ArgMustBeCngKeyBlobFormat";
        internal const string Cryptography_ArgMustBeCngProvider = "Cryptography_ArgMustBeCngProvider";
        internal const string Cryptography_DecryptWithNoKey = "Cryptography_DecryptWithNoKey";
        internal const string Cryptography_ECXmlSerializationFormatRequired = "Cryptography_ECXmlSerializationFormatRequired";
        internal const string Cryptography_InvalidAlgorithmGroup = "Cryptography_InvalidAlgorithmGroup";
        internal const string Cryptography_InvalidAlgorithmName = "Cryptography_InvalidAlgorithmName";
        internal const string Cryptography_InvalidCipherMode = "Cryptography_InvalidCipherMode";
        internal const string Cryptography_InvalidIVSize = "Cryptography_InvalidIVSize";
        internal const string Cryptography_InvalidKeyBlobFormat = "Cryptography_InvalidKeyBlobFormat";
        internal const string Cryptography_InvalidKeySize = "Cryptography_InvalidKeySize";
        internal const string Cryptography_InvalidPadding = "Cryptography_InvalidPadding";
        internal const string Cryptography_InvalidProviderName = "Cryptography_InvalidProviderName";
        internal const string Cryptography_MissingDomainParameters = "Cryptography_MissingDomainParameters";
        internal const string Cryptography_MissingPublicKey = "Cryptography_MissingPublicKey";
        internal const string Cryptography_MissingIV = "Cryptography_MissingIV";
        internal const string Cryptography_MustTransformWholeBlock = "Cryptography_MustTransformWholeBlock";
        internal const string Cryptography_NonCompliantFIPSAlgorithm = "Cryptography_NonCompliantFIPSAlgorithm";
        internal const string Cryptography_OpenInvalidHandle = "Cryptography_OpenInvalidHandle";
        internal const string Cryptography_OpenEphemeralKeyHandleWithoutEphemeralFlag = "Cryptography_OpenEphemeralKeyHandleWithoutEphemeralFlag";
        internal const string Cryptography_PartialBlock = "Cryptography_PartialBlock";
        internal const string Cryptography_PlatformNotSupported = "Cryptography_PlatformNotSupported";
        internal const string Cryptography_TlsRequiresLabelAndSeed = "Cryptography_TlsRequiresLabelAndSeed";
        internal const string Cryptography_TransformBeyondEndOfBuffer = "Cryptography_TransformBeyondEndOfBuffer";
        internal const string Cryptography_UnknownEllipticCurve = "Cryptography_UnknownEllipticCurve";
        internal const string Cryptography_UnknownEllipticCurveAlgorithm = "Cryptography_UnknownEllipticCurveAlgorithm";
        internal const string Cryptography_UnknownPaddingMode = "Cryptography_UnknownPaddingMode";
        internal const string Cryptography_UnexpectedXmlNamespace = "Cryptography_UnexpectedXmlNamespace";
        internal const string ArgumentException_RangeMinRangeMaxRangeType = "ArgumentException_RangeMinRangeMaxRangeType";
        internal const string ArgumentException_RangeNotIComparable = "ArgumentException_RangeNotIComparable";
        internal const string ArgumentException_RangeMaxRangeSmallerThanMinRange = "ArgumentException_RangeMaxRangeSmallerThanMinRange";
        internal const string ArgumentException_CountMaxLengthSmallerThanMinLength = "ArgumentException_CountMaxLengthSmallerThanMinLength";
        internal const string ArgumentException_LengthMaxLengthSmallerThanMinLength = "ArgumentException_LengthMaxLengthSmallerThanMinLength";
        internal const string ArgumentException_UnregisteredParameterName = "ArgumentException_UnregisteredParameterName";
        internal const string ArgumentException_InvalidParameterName = "ArgumentException_InvalidParameterName";
        internal const string ArgumentException_DuplicateName = "ArgumentException_DuplicateName";
        internal const string ArgumentException_DuplicatePosition = "ArgumentException_DuplicatePosition";
        internal const string ArgumentException_NoParametersFound = "ArgumentException_NoParametersFound";
        internal const string ArgumentException_HelpMessageBaseNameNullOrEmpty = "ArgumentException_HelpMessageBaseNameNullOrEmpty";
        internal const string ArgumentException_HelpMessageResourceIdNullOrEmpty = "ArgumentException_HelpMessageResourceIdNullOrEmpty";
        internal const string ArgumentException_HelpMessageNullOrEmpty = "ArgumentException_HelpMessageNullOrEmpty";
        internal const string ArgumentException_RegexPatternNullOrEmpty = "ArgumentException_RegexPatternNullOrEmpty";
        internal const string ArgumentException_RequiredPositionalAfterOptionalPositional = "ArgumentException_RequiredPositionalAfterOptionalPositional";
        internal const string ArgumentException_DuplicateParameterAttribute = "ArgumentException_DuplicateParameterAttribute";
        internal const string ArgumentException_MissingBaseNameOrResourceId = "ArgumentException_MissingBaseNameOrResourceId";
        internal const string ArgumentException_DuplicateRemainingArgumets = "ArgumentException_DuplicateRemainingArgumets";
        internal const string ArgumentException_TypeMismatchForRemainingArguments = "ArgumentException_TypeMismatchForRemainingArguments";
        internal const string ArgumentException_ValidationParameterTypeMismatch = "ArgumentException_ValidationParameterTypeMismatch";
        internal const string ArgumentException_ParserBuiltWithValueType = "ArgumentException_ParserBuiltWithValueType";
        internal const string InvalidOperationException_GetParameterTypeMismatch = "InvalidOperationException_GetParameterTypeMismatch";
        internal const string InvalidOperationException_GetParameterValueBeforeParse = "InvalidOperationException_GetParameterValueBeforeParse";
        internal const string InvalidOperationException_SetRemainingArgumentsParameterAfterParse = "InvalidOperationException_SetRemainingArgumentsParameterAfterParse";
        internal const string InvalidOperationException_AddParameterAfterParse = "InvalidOperationException_AddParameterAfterParse";
        internal const string InvalidOperationException_BindAfterBind = "InvalidOperationException_BindAfterBind";
        internal const string InvalidOperationException_GetRemainingArgumentsNotAllowed = "InvalidOperationException_GetRemainingArgumentsNotAllowed";
        internal const string InvalidOperationException_ParameterSetBeforeParse = "InvalidOperationException_ParameterSetBeforeParse";
        internal const string CommandLineParser_Aliases = "CommandLineParser_Aliases";
        internal const string CommandLineParser_ErrorMessagePrefix = "CommandLineParser_ErrorMessagePrefix";
        internal const string CommandLineParser_HelpMessagePrefix = "CommandLineParser_HelpMessagePrefix";
        internal const string ParameterBindingException_AmbiguousParameterName = "ParameterBindingException_AmbiguousParameterName";
        internal const string ParameterBindingException_ParameterValueAlreadySpecified = "ParameterBindingException_ParameterValueAlreadySpecified";
        internal const string ParameterBindingException_UnknownParameteName = "ParameterBindingException_UnknownParameteName";
        internal const string ParameterBindingException_RequiredParameterMissingCommandLineValue = "ParameterBindingException_RequiredParameterMissingCommandLineValue";
        internal const string ParameterBindingException_UnboundCommandLineArguments = "ParameterBindingException_UnboundCommandLineArguments";
        internal const string ParameterBindingException_UnboundMandatoryParameter = "ParameterBindingException_UnboundMandatoryParameter";
        internal const string ParameterBindingException_ResponseFileException = "ParameterBindingException_ResponseFileException";
        internal const string ParameterBindingException_ValididationError = "ParameterBindingException_ValididationError";
        internal const string ParameterBindingException_TransformationError = "ParameterBindingException_TransformationError";
        internal const string ParameterBindingException_AmbiguousParameterSet = "ParameterBindingException_AmbiguousParameterSet";
        internal const string ParameterBindingException_UnknownParameterSet = "ParameterBindingException_UnknownParameterSet";
        internal const string ParameterBindingException_NestedResponseFiles = "ParameterBindingException_NestedResponseFiles";
        internal const string ValidateMetadataException_RangeGreaterThanMaxRangeFailure = "ValidateMetadataException_RangeGreaterThanMaxRangeFailure";
        internal const string ValidateMetadataException_RangeSmallerThanMinRangeFailure = "ValidateMetadataException_RangeSmallerThanMinRangeFailure";
        internal const string ValidateMetadataException_PatternFailure = "ValidateMetadataException_PatternFailure";
        internal const string ValidateMetadataException_CountMinLengthFailure = "ValidateMetadataException_CountMinLengthFailure";
        internal const string ValidateMetadataException_CountMaxLengthFailure = "ValidateMetadataException_CountMaxLengthFailure";
        internal const string ValidateMetadataException_LengthMinLengthFailure = "ValidateMetadataException_LengthMinLengthFailure";
        internal const string ValidateMetadataException_LengthMaxLengthFailure = "ValidateMetadataException_LengthMaxLengthFailure";
        internal const string Argument_MapNameEmptyString = "Argument_MapNameEmptyString";
        internal const string Argument_EmptyFile = "Argument_EmptyFile";
        internal const string Argument_NewMMFWriteAccessNotAllowed = "Argument_NewMMFWriteAccessNotAllowed";
        internal const string Argument_ReadAccessWithLargeCapacity = "Argument_ReadAccessWithLargeCapacity";
        internal const string Argument_NewMMFAppendModeNotAllowed = "Argument_NewMMFAppendModeNotAllowed";
        internal const string ArgumentNull_MapName = "ArgumentNull_MapName";
        internal const string ArgumentNull_FileStream = "ArgumentNull_FileStream";
        internal const string ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed = "ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed";
        internal const string ArgumentOutOfRange_NeedPositiveNumber = "ArgumentOutOfRange_NeedPositiveNumber";
        internal const string ArgumentOutOfRange_PositiveOrDefaultCapacityRequired = "ArgumentOutOfRange_PositiveOrDefaultCapacityRequired";
        internal const string ArgumentOutOfRange_PositiveOrDefaultSizeRequired = "ArgumentOutOfRange_PositiveOrDefaultSizeRequired";
        internal const string ArgumentOutOfRange_PositionLessThanCapacityRequired = "ArgumentOutOfRange_PositionLessThanCapacityRequired";
        internal const string ArgumentOutOfRange_CapacityGEFileSizeRequired = "ArgumentOutOfRange_CapacityGEFileSizeRequired";
        internal const string IO_NotEnoughMemory = "IO_NotEnoughMemory";
        internal const string InvalidOperation_CalledTwice = "InvalidOperation_CalledTwice";
        internal const string InvalidOperation_CantCreateFileMapping = "InvalidOperation_CantCreateFileMapping";
        internal const string InvalidOperation_ViewIsNull = "InvalidOperation_ViewIsNull";
        internal const string NotSupported_DelayAllocateFileBackedNotAllowed = "NotSupported_DelayAllocateFileBackedNotAllowed";
        internal const string NotSupported_MMViewStreamsFixedLength = "NotSupported_MMViewStreamsFixedLength";
        internal const string ObjectDisposed_ViewAccessorClosed = "ObjectDisposed_ViewAccessorClosed";
        internal const string ObjectDisposed_StreamIsClosed = "ObjectDisposed_StreamIsClosed";
        internal const string NotSupported_Method = "NotSupported_Method";
        internal const string NotSupported_SubclassOverride = "NotSupported_SubclassOverride";
        internal const string Cryptography_ArgDSARequiresDSAKey = "Cryptography_ArgDSARequiresDSAKey";
        internal const string Cryptography_ArgRSAaRequiresRSAKey = "Cryptography_ArgRSAaRequiresRSAKey";
        internal const string Cryptography_CngKeyWrongAlgorithm = "Cryptography_CngKeyWrongAlgorithm";
        internal const string Cryptography_DSA_HashTooShort = "Cryptography_DSA_HashTooShort";
        internal const string Cryptography_HashAlgorithmNameNullOrEmpty = "Cryptography_HashAlgorithmNameNullOrEmpty";
        internal const string Cryptography_InvalidDsaParameters_MissingFields = "Cryptography_InvalidDsaParameters_MissingFields";
        internal const string Cryptography_InvalidDsaParameters_MismatchedPGY = "Cryptography_InvalidDsaParameters_MismatchedPGY";
        internal const string Cryptography_InvalidDsaParameters_MismatchedQX = "Cryptography_InvalidDsaParameters_MismatchedQX";
        internal const string Cryptography_InvalidDsaParameters_MismatchedPJ = "Cryptography_InvalidDsaParameters_MismatchedPJ";
        internal const string Cryptography_InvalidDsaParameters_SeedRestriction_ShortKey = "Cryptography_InvalidDsaParameters_SeedRestriction_ShortKey";
        internal const string Cryptography_InvalidDsaParameters_QRestriction_ShortKey = "Cryptography_InvalidDsaParameters_QRestriction_ShortKey";
        internal const string Cryptography_InvalidDsaParameters_QRestriction_LargeKey = "Cryptography_InvalidDsaParameters_QRestriction_LargeKey";
        internal const string Cryptography_InvalidRsaParameters = "Cryptography_InvalidRsaParameters";
        internal const string Cryptography_InvalidSignatureAlgorithm = "Cryptography_InvalidSignatureAlgorithm";
        internal const string Cryptography_KeyBlobParsingError = "Cryptography_KeyBlobParsingError";
        internal const string Cryptography_NotSupportedKeyAlgorithm = "Cryptography_NotSupportedKeyAlgorithm";
        internal const string Cryptography_NotValidPublicOrPrivateKey = "Cryptography_NotValidPublicOrPrivateKey";
        internal const string Cryptography_NotValidPrivateKey = "Cryptography_NotValidPrivateKey";
        internal const string Cryptography_UnexpectedTransformTruncation = "Cryptography_UnexpectedTransformTruncation";
        internal const string Cryptography_UnsupportedPaddingMode = "Cryptography_UnsupportedPaddingMode";
        internal const string Cryptography_WeakKey = "Cryptography_WeakKey";
        internal const string Cryptography_CurveNotSupported = "Cryptography_CurveNotSupported";
        internal const string Cryptography_InvalidCurve = "Cryptography_InvalidCurve";
        internal const string Cryptography_InvalidCurveOid = "Cryptography_InvalidCurveOid";
        internal const string Cryptography_InvalidCurveKeyParameters = "Cryptography_InvalidCurveKeyParameters";
        internal const string Cryptography_InvalidECCharacteristic2Curve = "Cryptography_InvalidECCharacteristic2Curve";
        internal const string Cryptography_InvalidECPrimeCurve = "Cryptography_InvalidECPrimeCurve";
        internal const string Cryptography_InvalidECNamedCurve = "Cryptography_InvalidECNamedCurve";
        internal const string Cryptography_UnknownHashAlgorithm = "Cryptography_UnknownHashAlgorithm";
        internal const string Argument_Invalid_SafeHandleInvalidOrClosed = "Argument_Invalid_SafeHandleInvalidOrClosed";
        internal const string Arg_EmptyOrNullArray = "Arg_EmptyOrNullArray";
        internal const string Arg_EmptyOrNullString = "Arg_EmptyOrNullString";
        internal const string Argument_InvalidOidValue = "Argument_InvalidOidValue";
        internal const string Cryptography_Cert_AlreadyHasPrivateKey = "Cryptography_Cert_AlreadyHasPrivateKey";
        internal const string Cryptography_CertReq_AlgorithmMustMatch = "Cryptography_CertReq_AlgorithmMustMatch";
        internal const string Cryptography_CertReq_BasicConstraintsRequired = "Cryptography_CertReq_BasicConstraintsRequired";
        internal const string Cryptography_CertReq_DatesReversed = "Cryptography_CertReq_DatesReversed";
        internal const string Cryptography_CertReq_DateTooOld = "Cryptography_CertReq_DateTooOld";
        internal const string Cryptography_CertReq_DuplicateExtension = "Cryptography_CertReq_DuplicateExtension";
        internal const string Cryptography_CertReq_IssuerBasicConstraintsInvalid = "Cryptography_CertReq_IssuerBasicConstraintsInvalid";
        internal const string Cryptography_CertReq_IssuerKeyUsageInvalid = "Cryptography_CertReq_IssuerKeyUsageInvalid";
        internal const string Cryptography_CertReq_IssuerRequiresPrivateKey = "Cryptography_CertReq_IssuerRequiresPrivateKey";
        internal const string Cryptography_CertReq_NoKeyProvided = "Cryptography_CertReq_NoKeyProvided";
        internal const string Cryptography_CertReq_NotAfterNotNested = "Cryptography_CertReq_NotAfterNotNested";
        internal const string Cryptography_CertReq_NotBeforeNotNested = "Cryptography_CertReq_NotBeforeNotNested";
        internal const string Cryptography_CertReq_RSAPaddingRequired = "Cryptography_CertReq_RSAPaddingRequired";
        internal const string Cryptography_Der_Invalid_Encoding = "Cryptography_Der_Invalid_Encoding";
        internal const string Cryptography_ECC_NamedCurvesOnly = "Cryptography_ECC_NamedCurvesOnly";
        internal const string Cryptography_Invalid_IA5String = "Cryptography_Invalid_IA5String";
        internal const string Cryptography_InvalidPaddingMode = "Cryptography_InvalidPaddingMode";
        internal const string Cryptography_InvalidPublicKey_Object = "Cryptography_InvalidPublicKey_Object";
        internal const string Cryptography_PrivateKey_DoesNotMatch = "Cryptography_PrivateKey_DoesNotMatch";
        internal const string Cryptography_PrivateKey_WrongAlgorithm = "Cryptography_PrivateKey_WrongAlgorithm";
        internal const string Cryptography_UnknownKeyAlgorithm = "Cryptography_UnknownKeyAlgorithm";
        private static SR loader;
        private ResourceManager resources;

        internal SR()
        {
            this.resources = new ResourceManager("System.Core", this.GetType().Assembly);
        }

        private static SR GetLoader()
        {
            if (SR.loader == null)
            {
                SR sr = new SR();
                Interlocked.CompareExchange<SR>(ref SR.loader, sr, (SR)null);
            }
            return SR.loader;
        }

        private static CultureInfo Culture {
            get {
                return (CultureInfo)null;
            }
        }

        public static ResourceManager Resources {
            get {
                return SR.GetLoader().resources;
            }
        }

        public static string GetString(string name, params object[] args)
        {
            SR loader = SR.GetLoader();
            if (loader == null)
                return (string)null;
            string format = loader.resources.GetString(name, SR.Culture);
            if (args == null || args.Length == 0)
                return format;
            for (int index = 0; index < args.Length; ++index)
            {
                if (args[index] is string str && str.Length > 1024)
                    args[index] = (object)(str.Substring(0, 1021) + "...");
            }
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, format, args);
        }

        public static string GetString(string name)
        {
            return SR.GetLoader()?.resources.GetString(name, SR.Culture);
        }

        public static string GetString(string name, out bool usedFallback)
        {
            usedFallback = false;
            return SR.GetString(name);
        }

        public static object GetObject(string name)
        {
            return SR.GetLoader()?.resources.GetObject(name, SR.Culture);
        }
    }
}
