"""Validate the signed AAB, signer certificate, and unchanged inspected payload."""
import hashlib, json, pathlib, re, ssl, subprocess, xml.etree.ElementTree as ET, zipfile
import PreflightClosedTestBundle as p
ROOT,OUT=p.ROOT,p.OUT
def command(args):
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8',errors='replace')
    assert result.returncode==0,result.stderr
    return result.stdout+result.stderr
def digest_entry(archive,name):
    digest=hashlib.sha256()
    with archive.open(name) as stream:
        while True:
            part=stream.read(1024*1024)
            if not part:break
            digest.update(part)
    return digest.digest()
def run():
    artifact=json.loads((OUT/'SignedArtifact.json').read_text(encoding='utf-8-sig'))
    signed=pathlib.Path(artifact['path'])
    verify=command([str(p.JAVA.parent/'jarsigner.exe'),'-verify','-verbose','-certs',str(signed)])
    (OUT/'JarsignerVerification.txt').write_text(verify,encoding='utf-8')
    cert=command([str(p.JAVA.parent/'keytool.exe'),'-J-Duser.language=en','-J-Duser.country=US','-printcert','-jarfile',str(signed)])
    (OUT/'SignerCertificate.txt').write_text(cert,encoding='utf-8')
    pem=(OUT/'UploadCertificate.pem').read_text()
    expected=hashlib.sha256(ssl.PEM_cert_to_DER_cert(pem)).hexdigest().upper()
    fingerprints=re.findall(r'SHA256:\s*([A-F0-9:]+)',cert)
    assert fingerprints and all(f.replace(':','')==expected for f in fingerprints)
    assert '3072-bit RSA key' in cert and 'SHA256withRSA' in cert
    validate=p.tool('validate','--bundle='+str(signed))
    (OUT/'BundletoolValidateSigned.txt').write_text(validate,encoding='utf-8')
    xml=p.tool('dump','manifest','--bundle='+str(signed),'--module=base')
    (OUT/'SignedManifest.xml').write_text(xml,encoding='utf-8')
    assert ET.tostring(ET.fromstring(xml))==ET.tostring(ET.fromstring((OUT/'UnsignedManifest.xml').read_text()))
    with zipfile.ZipFile(p.BUNDLE) as before,zipfile.ZipFile(signed) as after:
        payload={n for n in before.namelist() if not n.upper().startswith('META-INF/') and not n.endswith('/')}
        final_payload={n for n in after.namelist() if not n.upper().startswith('META-INF/') and not n.endswith('/')}
        assert payload==final_payload
        assert all(digest_entry(before,n)==digest_entry(after,n) for n in payload)
        signatures=[n for n in after.namelist() if n.upper().startswith('META-INF/') and n.upper().endswith('.RSA')]
        assert len(signatures)==1
    report=dict(passed=True,path=str(signed),bytes=signed.stat().st_size,
        sha256=hashlib.sha256(signed.read_bytes()).hexdigest().upper(),
        signerSha256=expected,signatureVerified=True,signerMatchesUserUploadCertificate=True,
        keyAlgorithm='RSA 3072 / SHA256withRSA',bundletoolValidated=True,payloadMatchesInspectedUnsignedBundle=True,
        packageName='com.colormovepuzzle.game',versionName=artifact['versionName'],versionCode=artifact['versionCode'],
        targetApi=36,minApi=26,development=False,channel='ClosedTestNoAds',adRequestsEnabled=False,umpEnabled=True)
    (OUT/'SignedVerification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report))
if __name__=='__main__':run()
