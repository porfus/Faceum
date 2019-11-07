from torchvision import models
import torchvision.transforms as transforms
from torchsummary import summary
import torch
import model_irse
import skimage as sk
import numpy as np





def de_preprocess(tensor):

    return tensor * 0.5 + 0.5

hflip = transforms.Compose([
            transforms.Normalize([0.5, 0.5, 0.5], [0.5, 0.5, 0.5])
        ])

def hflip_batch(imgs_tensor):
    hfliped_imgs = torch.empty_like(imgs_tensor)
    for i, img_ten in enumerate(imgs_tensor):
        hfliped_imgs[i] = hflip(img_ten)

    return hfliped_imgs

def load_model():
    device = torch.device("cuda")
    model = model_irse.IR_50(input_size = [112, 112]) 
    model.load_state_dict(torch.load('/models/backbone_ir50_ms1m_epoch120.pth', map_location="cuda:0"))
    model.to(device)
    model.eval()
    print(summary(model, (3,112,112),  device='cuda'))
    return model

def get_image_face_embedding(model, image_filenames):
    try:
        img_batch=[]
        for image_filename in image_filenames:
            try:
                img = sk.io.imread(image_filename)/float(255)
                img = np.asarray(img).transpose(-1, 0, 1)    
                img_batch.append(img)
            except BaseException as e:
                print("file error" +  str(e))
        img_batch_nm = np.array(img_batch)
        var_image = torch.tensor(img_batch_nm.astype(np.float32)).type('torch.cuda.FloatTensor').to(torch.device('cuda'))    
        img2=hflip_batch(var_image)    
        fff = model.forward(var_image).detach().cpu().numpy()
        return fff
    except:
        return None
    