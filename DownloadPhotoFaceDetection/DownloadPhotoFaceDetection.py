#from mtcnn.mtcnn import MTCNN
import cv2
import requests
import pymongo
import face_preprocess
from mtcnn_detector import MtcnnDetector
import numpy as np
import mxnet as mx
import os
from skimage import transform as trans
import scipy.misc
import hashlib
import re
from multiprocessing.dummy import Pool as ThreadPool
from multiprocessing import Process, Lock
from mtcnn.mtcnn import MTCNN






for i in range(4):
    mx.test_utils.download(dirname='mtcnn-model', url='https://s3.amazonaws.com/onnx-model-zoo/arcface/mtcnn-model/det{}-0001.params'.format(i+1))
    mx.test_utils.download(dirname='mtcnn-model', url='https://s3.amazonaws.com/onnx-model-zoo/arcface/mtcnn-model/det{}-symbol.json'.format(i+1))
    mx.test_utils.download(dirname='mtcnn-model', url='https://s3.amazonaws.com/onnx-model-zoo/arcface/mtcnn-model/det{}.caffemodel'.format(i+1))
    mx.test_utils.download(dirname='mtcnn-model', url='https://s3.amazonaws.com/onnx-model-zoo/arcface/mtcnn-model/det{}.prototxt'.format(i+1))

if len(mx.test_utils.list_gpus())==0:
    ctx = mx.cpu()
else:
    ctx = mx.gpu(0)
# Configure face detector
det_threshold = [0.6,0.7,0.8]
mtcnn_path = os.path.join(os.path.dirname('__file__'), 'mtcnn-model')

detector = MtcnnDetector(model_folder=mtcnn_path, ctx=ctx, num_worker=4, accurate_landmark = True, threshold=det_threshold)
detectorTest = MTCNN()

def preprocess(img, bbox=None, landmark=None, **kwargs):
    M = None
    image_size = []
    str_image_size = kwargs.get('image_size', '')
    # Assert input shape
    if len(str_image_size)>0:
        image_size = [int(x) for x in str_image_size.split(',')]
        if len(image_size)==1:
            image_size = [image_size[0], image_size[0]]
        assert len(image_size)==2
        assert image_size[0]==112
        assert image_size[0]==112 or image_size[1]==96
    
    # Do alignment using landmark points
    if landmark is not None:
        assert len(image_size)==2
        src = np.array([
          [30.2946, 51.6963],
          [65.5318, 51.5014],
          [48.0252, 71.7366],
          [33.5493, 92.3655],
          [62.7299, 92.2041] ], dtype=np.float32 )
        if image_size[1]==112:
            src[:,0] += 8.0
        dst = landmark.astype(np.float32)
        tform = trans.SimilarityTransform()
        tform.estimate(dst, src)
        M = tform.params[0:2,:]
        assert len(image_size)==2
        warped = cv2.warpAffine(img,M,(image_size[1],image_size[0]), borderValue = 0.0)
        return warped
    
    # If no landmark points available, do alignment using bounding box. If no bounding box available use center crop
    if M is None:
        if bbox is None:
            det = np.zeros(4, dtype=np.int32)
            det[0] = int(img.shape[1]*0.0625)
            det[1] = int(img.shape[0]*0.0625)
            det[2] = img.shape[1] - det[0]
            det[3] = img.shape[0] - det[1]
        else:
            det = bbox
        margin = kwargs.get('margin', 44)
        bb = np.zeros(4, dtype=np.int32)
        bb[0] = np.maximum(det[0]-margin/2, 0)
        bb[1] = np.maximum(det[1]-margin/2, 0)
        bb[2] = np.minimum(det[2]+margin/2, img.shape[1])
        bb[3] = np.minimum(det[3]+margin/2, img.shape[0])
        ret = img[bb[1]:bb[3],bb[0]:bb[2],:]
        if len(image_size)>0:
            ret = cv2.resize(ret, (image_size[1], image_size[0]))
        return ret
lock = Lock()
def process_photo(url):
    response = requests.get(url)
    if response.status_code == 200:
       
        img = cv2.imdecode(np.frombuffer(response.content, np.uint8), -1)      
        img = cv2.cvtColor(img , cv2.COLOR_BGR2RGB)
        if len(img.shape) != 3:
            return None
        #lock.acquire()
        ret = detector.detect_face(img, det_type = 0)
        retTest = detectorTest.detect_faces(img)
        #lock.release()        
        if ret is None:
            return None
        bboxs, pointes = ret
        if bboxs.shape[0]==0:
            return None
            
        bboxsOut=[]
        for i in range(len(bboxs)):
            bbox=bboxs[i]
            score=bbox[-1]
            if score <0.98:
                continue
            bbox = bbox[0:4]    
            points = pointes[i].reshape((2,5)).T
            # Call preprocess() to generate aligned images
            nimg = preprocess(img, bbox, points, image_size='112,112')
            nimg = cv2.cvtColor(nimg, cv2.COLOR_BGR2RGB)              
            aligned = np.transpose(nimg, (2,0,1))

            bytesToHash=nimg.flatten().tolist()
            m = hashlib.md5()
            m.update(url.encode('utf-8'))
            filenameOut=m.hexdigest()+'_'+str(i)
            folder1=filenameOut[0]
            folder2=filenameOut[1]
            folder3=filenameOut[2]
            saveFolderName=os.path.abspath(os.path.join('..\\','face_db',folder1))
            if not os.path.exists(saveFolderName):
                os.mkdir(saveFolderName)
            saveFolderName=os.path.join(saveFolderName,folder2)
            if not os.path.exists(saveFolderName):
                os.mkdir(saveFolderName)
              
            saveFolderName=os.path.join(saveFolderName,folder3)
            if not os.path.exists(saveFolderName):
                os.mkdir(saveFolderName)



            saveFileName=os.path.join(saveFolderName,filenameOut+'.jpg')
            cv2.imwrite(saveFileName,nimg)
            bboxsOut.append({'box':bbox.tolist(),'filename':filenameOut})
            
        return bboxsOut


totalProcessed=0
def process_people(people):
    photos=people['Photos']    
    for photo in photos:
        if photo == '':
            continue
        myquery = { "photo": photo }
        disablePhotoCheked=False       
        if collectionPhotos.count_documents(myquery) == 0:    
            try:
                faces=process_photo(photo)
                photoInfo={'photo':photo, 'faces':faces}
                collectionPhotos.insert_one(photoInfo)
            except:
                print('Error')



from pymongo import MongoClient
client = MongoClient('localhost', 27017)
db = client['vk']
collectionPeoples = db['peoples']
collectionPhotos = db['photos']
regx = re.compile("Орен", re.IGNORECASE)
peopleToProcessing=list(collectionPeoples.find({'UserCity':regx}))
pool = ThreadPool(1)

pool.map(process_people, peopleToProcessing)

#close the pool and wait for the work to finish 
pool.close()
pool.join()

